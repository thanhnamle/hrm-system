# Application Layer — CQRS + MediatR

Diagram: [diagrams/clean-architecture.drawio](diagrams/clean-architecture.drawio) (page "Request flow")

## 1. How the patterns combine

| Pattern | Answers | Role in this project |
|---|---|---|
| Clean Architecture | *Where* does code live? | 4 projects, dependencies point inward |
| CQRS | *What shape* is each operation? | One Command or Query class per use case |
| MediatR | *How* does a request reach its handler? | Controllers only depend on `ISender`; pipeline behaviors handle cross-cutting concerns |

- **Commands** change state: go through validation → domain rules → `SaveChanges`.
- **Queries** read state: `AsNoTracking()` + `.Select()` projection straight to DTOs, skipping the Domain.

## 2. Folder structure (feature folders)

```
HRM.Application/
├── Common/
│   ├── Behaviors/      LoggingBehavior.cs, ValidationBehavior.cs
│   ├── Interfaces/     IApplicationDbContext, ICurrentUser, IIdentityService, IJwtTokenService, IRefreshTokenService
│   ├── Exceptions/     NotFound, Forbidden, Unauthorized, Validation
│   ├── Models/         PagedResult<T>, PageRequest
│   └── Security/       Roles, Permissions, RolePermissions, DataScope
├── Features/
│   ├── Auth/           Login/, RefreshToken/, Logout/, GetMe/
│   ├── Employees/      Commands/{Create,Update,Terminate}Employee/   Queries/{GetEmployees,GetEmployeeById}/
│   ├── Departments/    Commands/{Create,Update,Delete,AssignManager}/ Queries/{GetDepartments}/
│   ├── LeaveRequests/  Commands/{Submit,Cancel,Approve,Reject}/      Queries/{GetMine,GetPendingApprovals,GetAll}/
│   ├── Attendance/     Commands/{CheckIn,CheckOut}/                   Queries/{GetMine,GetTeam}/
│   ├── Users/          Commands/{CreateAccount,ChangeRole,Lock}/      Queries/{GetUsers}/
│   └── Dashboard/      Queries/GetDashboard/
└── DependencyInjection.cs
```

Each use-case folder: `XxxCommand.cs`, `XxxHandler.cs`, `XxxValidator.cs`.

## 3. Pipeline

```
Send(request) → LoggingBehavior → ValidationBehavior → Handler → SaveChanges (single transaction)
```

No separate transaction behavior: each command calls `SaveChangesAsync` once, which EF Core makes atomic.

```csharp
// HRM.API/Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
```

## 4. API endpoints

| Method | Endpoint | Policy | Scope |
|---|---|---|---|
| POST | `/api/auth/login` | Anonymous (rate-limited) | |
| POST | `/api/auth/refresh` | Anonymous (cookie) | |
| POST | `/api/auth/logout` | Authenticated | |
| GET | `/api/auth/me` | Authenticated | self |
| GET | `/api/employees?search=&page=` | `Employees.View` | role scope |
| GET | `/api/employees/{id}` | `Employees.View` | role scope |
| POST / PUT | `/api/employees`, `/api/employees/{id}` | `Employees.Create` / `Employees.Update` | |
| POST | `/api/employees/{id}/terminate` | `Employees.Terminate` | |
| GET | `/api/departments` | `Departments.View` | |
| POST / PUT / DELETE | `/api/departments/...` | `Departments.Manage` | |
| GET | `/api/leave-requests/mine` | `Leave.Request` | self |
| POST | `/api/leave-requests` | `Leave.Request` | self |
| POST | `/api/leave-requests/{id}/cancel` | `Leave.Request` | owner only |
| GET | `/api/leave-requests/pending` | `Leave.Approve` | own dept / all |
| POST | `/api/leave-requests/{id}/approve`, `/reject` | `Leave.Approve` | own dept, not self |
| GET | `/api/leave-requests` | `Leave.ViewAll` | |
| POST | `/api/attendance/check-in`, `/check-out` | `Attendance.Record` | self |
| GET | `/api/attendance/mine` | `Attendance.Record` | self |
| GET | `/api/attendance?date=` | `Attendance.View` | role scope |
| GET / POST / PUT | `/api/users/...` | `Users.Manage` | |
| GET | `/api/dashboard` | Authenticated | role-dependent content |
