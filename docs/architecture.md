# Architecture

## 1. System architecture (deployment)

Diagram: [diagrams/system-architecture.drawio](diagrams/system-architecture.drawio) — pages *Detailed*, *Overview*, *Local development*

- **Browser** runs the Angular SPA.
- **Nginx** serves the Angular build at `/` and reverse-proxies `/api/*` to the API. Same origin → no CORS headaches, and the refresh-token cookie can be `SameSite=Strict`.
- **ASP.NET Core API** (.NET 10) — the only component that talks to the database.
- **PostgreSQL 17** with a named Docker volume.

All three server components run in Docker Compose.

### Ports & protocols

| From → To | Protocol / port | Exposure |
|---|---|---|
| Browser → Nginx | HTTP, container :80 → host `WEB_PORT` (8080) | Published to host |
| Nginx → API | HTTP :8080 (`/api/*`, `/health`) | Internal (compose network) |
| API → PostgreSQL | TCP :5432 (Npgsql) | Internal; also `127.0.0.1:DB_PORT` for development tools |

The Docker setup is a local demo over plain HTTP. In production TLS terminates in front of (or in) Nginx on :443, and HSTS is switched on in `hrm-client/nginx/security-headers.conf`.

### Request path (numbers match the *Detailed* diagram)

1. Angular interceptor sends the request + Bearer JWT to Nginx (same origin).
2. Nginx proxies `/api/*` to the API on :8080, replacing `X-Forwarded-For` with the client IP; the API reads it (`ReverseProxy:Enabled`) so the login rate limiter sees real clients.
3. Middleware (exceptions → Serilog → rate limiter → JWT → policies) reaches the controller, which calls `ISender.Send()`.
4. MediatR behaviors run, then the handler applies Domain rules.
5. Handler persists through `IApplicationDbContext` (implemented by `AppDbContext`).
6. EF Core writes to PostgreSQL in one transaction; the response returns as JSON (plus `Set-Cookie` on auth endpoints).

### Cross-cutting concerns

| Concern | How |
|---|---|
| Logging | Serilog → stdout → `docker logs` |
| Health | `/health` checks API + PostgreSQL |
| Secrets | `.env` → env vars (`DB_PASSWORD`, `JWT_KEY`, `SEED_PASSWORD`) |
| Network | Only `web` is published; `api` is internal, `db` only on localhost |
| Security headers | Nginx: CSP (`script-src 'self'`), `X-Frame-Options: DENY`, `nosniff`, Referrer- and Permissions-Policy |
| Tests | xUnit, Testcontainers (real PostgreSQL) |

## 2. Backend — Clean Architecture

Diagram: [diagrams/clean-architecture.drawio](diagrams/clean-architecture.drawio)

| Layer | Project | Contains | Depends on |
|---|---|---|---|
| Domain | `HRM.Domain` | Entities, enums, business rules, `DomainException` | nothing |
| Application | `HRM.Application` | Commands/Queries/Handlers/Validators, pipeline behaviors, interfaces, DTOs, permissions | Domain |
| Infrastructure | `HRM.Infrastructure` | `AppDbContext` (EF Core + Npgsql), Identity, `JwtTokenService`, `CurrentUser`, audit interceptor, migrations, seed | Application |
| Presentation | `HRM.API` | Controllers, authorization policies, exception handler, `Program.cs` (DI composition root) | Application, Infrastructure |

**Dependency rule:** source-code dependencies point inward only. `AppUser` (ASP.NET Core Identity) lives in Infrastructure, linked to the Domain `Employee` by `EmployeeId`, so the Domain stays framework-free.

## 3. Domain model

Diagram: [diagrams/erd.drawio](diagrams/erd.drawio)

**Business rules (in Domain):**
- Leave is counted in **working days (Mon–Fri)**; public holidays are not excluded yet. New hires get **12 days** of annual leave.
- Annual leave days ≤ `LeaveBalance − pending annual days`; balance is deducted on **approve**. Sick / Unpaid leave do not use the balance.
- Leave cannot start in the past or overlap a pending/approved request.
- Only a `Pending` request can be approved / rejected / cancelled.
- **Nobody can approve their own leave request.**
- One check-in per employee per day; check-out must be after check-in.
- A terminated employee cannot log in or submit requests.

All tables carry `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` (set by an EF Core interceptor). Employees are soft-deleted (`Status = Terminated`).

## 4. Repository layout

```
HR Management System/
├── hrm-server/                      (.NET backend)
│   ├── hrm-server.slnx
│   ├── Directory.Build.props        (net10.0, nullable, warnings as errors)
│   ├── Directory.Packages.props     (all NuGet versions in one place)
│   ├── src/
│   │   ├── HRM.Domain/              (no references)
│   │   ├── HRM.Application/         → Domain
│   │   ├── HRM.Infrastructure/      → Application
│   │   └── HRM.API/                 → Application, Infrastructure
│   └── tests/
│       ├── Directory.Build.props    (xUnit packages for all test projects)
│       ├── HRM.Domain.Tests/        → Domain
│       ├── HRM.Application.Tests/   → Application
│       └── HRM.API.IntegrationTests/ → API
├── hrm-client/                      (Angular 22 + Tailwind v4, see hrm-client/README.md)
├── docs/
├── docker-compose.yml               (db on Day 1; api + web on Day 10)
├── .env.example
└── README.md
```

## 5. Infrastructure

### Backend packages

All versions are pinned in `hrm-server/Directory.Packages.props`.

| Purpose | Package | Added |
|---|---|---|
| Mediator | `MediatR` **12.5.0, pinned** (last Apache-2.0 release; v13+ is commercial — do not upgrade) | Day 1 |
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` + `EFCore.NamingConventions` (snake_case) | Day 1 |
| Health | `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (`AddDbContextCheck`) | Day 1 |
| Logging | `Serilog.AspNetCore` | Day 1 |
| API docs | `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` | Day 1 |
| Tests | `xUnit` + `Shouldly` (+ `Testcontainers.PostgreSql` on Day 5) | Day 1 / 5 |
| Migrations | `Microsoft.EntityFrameworkCore.Design` + `dotnet-ef` tool | Day 2 |
| Identity | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Day 2 |
| JWT | `Microsoft.AspNetCore.Authentication.JwtBearer` | Day 3 |
| Validation | `FluentValidation.DependencyInjectionExtensions` (moved up from Day 4: login is the first validated command) | Day 3 |
| Test doubles | `NSubstitute` | Day 3 |

DTO mapping is done manually with `.Select(...)` projections (no AutoMapper).

### Docker images and compose

The real file is [docker-compose.yml](../docker-compose.yml); the README has the quick start.

| Service | Image | Notes |
|---|---|---|
| `db` | `postgres:17` | Named volume `pgdata`, `pg_isready` healthcheck |
| `api` | `hrm-server/src/HRM.API/Dockerfile`: `dotnet/sdk:10.0` build → `dotnet/aspnet:10.0` runtime (~360 MB) | Runs as the non-root `app` user on :8080. Restore is a separate layer, cached until a `.csproj` changes. Env: connection string, `Jwt__Key`, `Database__MigrateOnStartup`, `Database__SeedDemoData`, `ReverseProxy__Enabled` |
| `web` | `hrm-client/Dockerfile`: `node:24-alpine` build (`npm ci`, `ng build`) → `nginx:1.30-alpine` (~95 MB) | Config in `hrm-client/nginx/`. Hashed bundles cached for a year, `index.html` never; unknown paths fall back to `index.html`. Healthcheck calls `/health` through Nginx, so *healthy* means the whole chain works |

Migrations and demo data run on startup only in Development, or when the two `Database__*` flags are on (the Docker demo). A real deployment runs migrations as a release step and never seeds.

The production build sets `inlineCritical: false`: Angular's critical-CSS inlining loads the stylesheet with an inline script, which the CSP blocks.

### Local development

- PostgreSQL in Docker, API with `dotnet watch`, Angular with `ng serve` + `proxy.conf.json` forwarding `/api` (mimics Nginx same-origin).
- Migrations via `dotnet ef`.
- Seed: 4 roles, 1 admin, 3 departments, ~10 sample employees with accounts.
- Secrets: `dotnet user-secrets` in development, `.env` (gitignored) for Docker; commit `.env.example` only.

## 6. Frontend structure (Angular + Tailwind)

Angular 22 (standalone, signals, zoneless, OnPush by default), Tailwind CSS v4 (tokens in `src/styles.css`), Vitest, ESLint + Prettier.

**Environments.** The browser always calls its own origin at `/api`: `proxy.conf.json` forwards it to `http://localhost:5183` during `ng serve`, Nginx forwards it to the API container in production. Both `environment.ts` files therefore use `apiBaseUrl: '/api'`; they hold no secrets because the whole bundle is public.

**Layering:** `features` use `shared` and `core`. `core`, `shared` and `layout` never import `features` (an ESLint error). Features don't import each other (a convention, checked in review).

```
hrm-client/src/app/
├── app.config.ts · app.routes.ts    providers; top-level routes, every page lazy-loaded
├── core/
│   ├── auth/        permissions.ts (same names as the API), auth.service.ts (signals: user, permissions),
│   │                auth.interceptor.ts, refresh.interceptor.ts, auth.guard.ts, permission.guard.ts
│   ├── http/        error.interceptor.ts (ProblemDetails → toast)
│   └── models/      DTO interfaces matching the API (common, auth, employee, department, leave, attendance, dashboard, user)
├── shared/
│   ├── ui/          button, input, table, pagination, modal, badge, toast (Tailwind)
│   └── directives/  has-permission.directive.ts
├── layout/          shell (sidebar filtered by permission + topbar)
└── features/        (all lazy-loaded)
    ├── auth/        login
    ├── dashboard/
    ├── employees/   list, detail, form
    ├── departments/
    ├── leave/       my-requests, request-form, approvals
    ├── attendance/  check-in, history, team
    └── admin/users/
```

**Dashboard by role**

One call (`GET /api/dashboard`); the API fills only the sections the role gets. The page reloads it every minute and shows when it last updated and how long the call took.

| Role | Sections |
|---|---|
| Admin / HR | **Organization:** headcount with hires/leavers, present today, on leave, approvals waiting; attendance today as a segmented bar; headcount by department with managers; oldest pending requests; who's away in the next 14 days; leave days by type this year; recent job changes. **Reliability:** system status (database health check with timings, API round trip from the browser, uptime, version) and data quality (logins, CVs, skills, education, locked accounts, accounts by role, who needs attention) |
| Manager | **My team:** team size, checked in, on leave, requests to review; each member's status today with times; pending requests; upcoming team leave |
| Anyone with an employee record | **Me:** position, department, tenure, profile completeness, last job change; annual leave available, leave taken this year, next leave; today's check-in and days present / hours this month; recent requests |

**Data storytelling.** A banner at the top says what matters today in a sentence, with one hero figure and points that link to where to act; every chart title states its takeaway ("Attendance averaged 94% over the last 21 working days"). The sentences are pure functions of the API data (`features/dashboard/story.ts`). Charts are hand-built SVG (no chart library, CSP-safe): a 30-day attendance trend with a crosshair tooltip, approved leave by month stacked by type (validated three-colour palette, legend, "upcoming" divider), headcount over 12 months, and hours per week for the employee. Each chart has keyboard tooltips and a "View as table" fallback. Motion (count-up numbers, staggered entrance, lines drawing, columns growing) switches off under `prefers-reduced-motion`.

**Demo history.** With demo seeding on (Development, or `Database__SeedDemoData` in Docker), `DemoHistorySeeder` adds about 90 days of attendance and six months of approved leave through the domain methods, so the charts have a story on a fresh install. It runs once (skipped when older attendance exists) and never in tests or production.

The system status comes from `ISystemHealth` (Application interface, implemented in Infrastructure with ASP.NET Core's `HealthCheckService`), so the dashboard stays a normal MediatR query and needs no new permission.
