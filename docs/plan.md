# Project Plan — 2 Weeks

**Assumptions:** 1 developer, 10 working days, .NET 10 (LTS), latest Angular, Tailwind v4, PostgreSQL 17.

## 1. Scope

| ✅ In scope (MVP) | ❌ Out of scope (phase 2) |
|---|---|
| Authentication (login, refresh token, logout) | Payroll |
| Users & roles (Admin, HR, Manager, Employee) | Recruitment |
| Employees (CRUD, search, paging, terminate) | Performance reviews |
| Departments (CRUD, assign manager) | Email notifications |
| Leave management (request, approve/reject, balance) | File uploads (CV, contracts) |
| Attendance (check-in / check-out, history) | Multi-company / multi-tenancy |
| Role-based dashboard | |

## 2. Day-by-day schedule

### Week 1 — Backend

| Day | Work | Done when |
|---|---|---|
| **1. Foundation** | Solution + 4 projects + test projects, project references, PostgreSQL in Docker, Serilog, OpenAPI + Scalar, global exception handler, `/health`, base entity + audit interceptor, Git repo | `dotnet run` works, `/health` green, Scalar UI opens |
| **2. Domain + Persistence** | Entities & business rules, EF configurations, Identity tables (`AppUser`), first migration, seed data | DB tables exist, seed accounts exist, domain unit tests pass |
| **3. Auth + RBAC** | Login, refresh (rotation + reuse detection), logout, `/me`, JWT service, `ICurrentUser`, permissions + `RolePermissions` + policies, rate limiter, lockout | Each role logs in and gets correct `permissions`; wrong role → 403 |
| **4. Employees + Departments** | MediatR registration + behaviors, all commands/queries, validators, paging/search, `ApplyScope` | Manager sees only own department; Employee sees only self |
| **5. Leave + Attendance + Dashboard** | Submit/cancel/approve/reject, balance rules, check-in/out, dashboard query, **integration tests per role on sensitive endpoints** | All tests green; full API usable from Scalar |

### Week 2 — Frontend + Integration

| Day | Work | Done when |
|---|---|---|
| **6. Angular foundation** | Project, Tailwind v4, folder structure, proxy, shell layout, login page, `AuthService` (signals), interceptors (auth, refresh, error), guards, `hasPermission` directive | Log in → sidebar shows only allowed menus; reload keeps session via refresh |
| **7. Employees + Departments UI** | Reusable table + paging + search, employee list/detail/form (reactive forms + validation), departments CRUD | HR does full CRUD; Manager sees read-only team list |
| **8. Leave + Attendance UI** | My requests, request form (shows balance), approvals inbox, check-in/out button, history, team view | Full leave flow works end-to-end across two browser sessions |
| **9. Dashboard + Admin + Hardening** | Role dashboards, user management (create account, change role, lock), `/forbidden` page, security pass | Security checklist (§4) passes |
| **10. Ship** | Dockerfiles, `docker compose up` full stack, Nginx config with security headers, README, manual test per role, **bug-fix buffer** | Fresh clone + `docker compose up` = working system |

## 3. If behind schedule — cut in this order

1. Attendance team view
2. Admin user UI (seed users instead)
3. Dashboard charts (plain numbers are fine)
4. Attendance entirely

**Never cut:** authentication, RBAC layers 1 & 2, leave flow.

## 4. Final checklist (Day 9–10)

- [ ] Every controller action has `[Authorize(Policy = ...)]` or explicit `[AllowAnonymous]`
- [ ] Every employee-data query calls `ApplyScope`
- [ ] Manager cannot approve own leave or another department's leave (integration test)
- [ ] Employee calling `GET /api/employees/{otherId}` → 404/403 (integration test)
- [ ] Access token not in `localStorage`; refresh cookie is `HttpOnly`
- [ ] Reused refresh token logs user out everywhere
- [ ] Login is rate-limited; account locks after 5 failures
- [ ] No secrets committed (`.env`, keys in `appsettings*.json`)
- [ ] 500 responses contain no stack trace
- [ ] Terminated employee cannot log in

## 5. Testing strategy

| Level | Project | What |
|---|---|---|
| Unit | `HRM.Domain.Tests` | Business rules: leave balance, status transitions, attendance rules |
| Unit | `HRM.Application.Tests` | Handlers with mocked `ICurrentUser`, data scope logic |
| Integration | `HRM.API.IntegrationTests` | `WebApplicationFactory` + Testcontainers PostgreSQL; one test per role per sensitive endpoint |
