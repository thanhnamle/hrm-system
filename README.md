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

## Documentation

Start at [docs/README.md](docs/README.md). The 2-week schedule is in [docs/plan.md](docs/plan.md).
