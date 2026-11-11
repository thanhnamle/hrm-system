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
| Browser → Nginx | HTTPS :443 (HTTP :80 redirects) | Published to host |
| Nginx → API | HTTP :8080 (`/api/*`) | Internal (`hrm-net`) |
| API → PostgreSQL | TCP :5432 (Npgsql) | Internal (`hrm-net`) |

### Request path (numbers match the *Detailed* diagram)

1. Angular interceptor sends HTTPS request + Bearer JWT to Nginx.
2. Nginx proxies `/api/*` to the API on :8080.
3. Middleware (exceptions → Serilog → rate limiter → JWT → policies) reaches the controller, which calls `ISender.Send()`.
4. MediatR behaviors run, then the handler applies Domain rules.
5. Handler persists through `IApplicationDbContext` (implemented by `AppDbContext`).
6. EF Core writes to PostgreSQL in one transaction; the response returns as JSON (plus `Set-Cookie` on auth endpoints).

### Cross-cutting concerns

| Concern | How |
|---|---|
| Logging | Serilog → stdout → `docker logs` |
| Health | `/health` checks API + PostgreSQL |
| Secrets | `.env` → env vars (`DB_PASSWORD`, `JWT_KEY`) |
| Network | Only `web` is published; `api` and `db` stay on `hrm-net` |
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
├── docker-compose.yml               (Day 1)
├── .env.example                     (Day 1)
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

### docker-compose.yml (outline)

```yaml
services:
  db:
    image: postgres:17
    environment:
      POSTGRES_DB: hrm
      POSTGRES_USER: hrm
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes: [pgdata:/var/lib/postgresql/data]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U hrm"]
      interval: 5s
  api:
    build: { context: ./hrm-server, dockerfile: src/HRM.API/Dockerfile }
    environment:
      ConnectionStrings__Default: Host=db;Database=hrm;Username=hrm;Password=${DB_PASSWORD}
      Jwt__Key: ${JWT_KEY}
    depends_on:
      db: { condition: service_healthy }
  web:
    build: ./hrm-client            # multi-stage: node build → nginx
    ports: ["8080:80"]
    depends_on: [api]
volumes:
  pgdata:
```

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

| Role | Widgets |
|---|---|
| Admin / HR | Headcount, headcount by department, on leave today, pending approvals |
| Manager | Team size, pending approvals, team attendance today |
| Employee | Leave balance, my recent requests, today's check-in status |
