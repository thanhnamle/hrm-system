# HR Management System

A simple HR Management System: employees, departments, leave requests, attendance, and role-based access (Admin, HR, Manager, Employee).

| Part | Stack |
|---|---|
| Backend (`hrm-server/`) | C# · ASP.NET Core (.NET 10) · Clean Architecture · CQRS · MediatR · EF Core |
| Frontend (`hrm-client/`) | Angular · TypeScript · Tailwind CSS |
| Database | PostgreSQL 17 |
| Infrastructure | Docker Compose · Nginx |

## Repository layout

```
hrm-server/   .NET solution (src/ + tests/)
hrm-client/   Angular app (coming on Day 6)
docs/         plan, architecture, security, RBAC, draw.io diagrams
```

## Getting started (backend)

Requirements: .NET 10 SDK, Docker Desktop.

1. **Create your local secrets** (once). Copy `.env.example` to `.env` and set `DB_PASSWORD`, then give the API the same password:

   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=hrm;Username=hrm;Password=<DB_PASSWORD>" --project hrm-server/src/HRM.API
   ```

   Also set the password used for every seeded demo account (8+ chars, with upper, lower, digit and symbol):

   ```bash
   dotnet user-secrets set "Seed:DefaultPassword" "<password>" --project hrm-server/src/HRM.API
   ```

   And a random JWT signing key (at least 32 bytes; the API refuses to start without it):

   ```bash
   dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project hrm-server/src/HRM.API
   ```

2. **Start PostgreSQL:**

   ```bash
   docker compose up -d db
   ```

3. **Build, test, run:**

   ```bash
   cd hrm-server
   dotnet build hrm-server.slnx
   dotnet test hrm-server.slnx
   dotnet run --project src/HRM.API --launch-profile http
   ```

| URL | What |
|---|---|
| http://localhost:5183/health | `Healthy` when the API can reach PostgreSQL (503 otherwise) |
| http://localhost:5183/scalar | Interactive API docs (Development only) |
| http://localhost:5183/openapi/v1.json | OpenAPI document (Development only) |

In Development the API applies pending EF Core migrations and seeds demo data on startup (skipped if data already exists).

### Signing in

`POST /api/auth/login` with `{ "email": "...", "password": "..." }` returns a 15-minute `accessToken` plus the user's role and `permissions`, and sets the refresh token as an HttpOnly cookie (`hrm_refresh`, path `/api/auth`). In Scalar, paste the access token into the Bearer auth box. `POST /api/auth/refresh` renews both tokens, `POST /api/auth/logout` revokes the refresh token, `GET /api/auth/me` returns the current user. Every other endpoint requires a token; see [docs/rbac.md](docs/rbac.md) for who may call what.

Login is limited to 5 attempts per minute per IP, and 5 wrong passwords lock an account for 15 minutes.

### Testing with Postman

Import both files from [postman/](postman/): `HRM.postman_collection.json` and `HRM.local.postman_environment.json`. Select the **HRM Local** environment, set `password` to your `Seed:DefaultPassword`, start the API, then run the collection (Collection Runner) or send requests one by one starting with **00 Setup**. It covers every endpoint for every role, including the 403/404 cases. Folders 00–06 use five logins, so wait a minute before **07 Logout** (rate limit).

To start over with fresh demo data, drop the database volume: `docker compose down -v` then `docker compose up -d db` (the API re-seeds on its next start).

### Demo accounts (Development seed)

All use the `Seed:DefaultPassword` from your user-secrets.

| Role | Accounts |
|---|---|
| Admin | `admin@hrm.local` (no employee record) |
| HR | `linh.tran@hrm.local` (manages HR), `mai.pham@hrm.local` |
| Manager | `minh.nguyen@hrm.local` (Engineering), `lan.bui@hrm.local` (Sales) |
| Employee | `huy.le`, `thao.vo`, `khoa.dang`, `tuan.phan` (Engineering) · `quang.do`, `ngoc.hoang` (Sales) — all `@hrm.local` |

### Database migrations

`dotnet-ef` is a local tool (`hrm-server/dotnet-tools.json`). From `hrm-server/`:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/HRM.Infrastructure --startup-project src/HRM.API --output-dir Persistence/Migrations
dotnet ef database update --project src/HRM.Infrastructure --startup-project src/HRM.API
```

## Documentation

Start at [docs/README.md](docs/README.md). The 2-week schedule is in [docs/plan.md](docs/plan.md).
