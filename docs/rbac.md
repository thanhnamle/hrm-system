# Role-Based Access Control (RBAC)

Diagrams: [diagrams/rbac-layers.drawio](diagrams/rbac-layers.drawio), [diagrams/sequence-approve-leave.drawio](diagrams/sequence-approve-leave.drawio)

## 1. Roles

| Role | Description |
|---|---|
| **Admin** | Full access, manages user accounts and roles |
| **HR** | Manages employees, departments, all leave and attendance |
| **Manager** | Manages own department's team: views team, approves team leave |
| **Employee** | Self-service: own profile, leave requests, attendance |

One role per user (keeps the model simple).

## 2. Three layers of access checks

| Layer | Where | Question | Result on failure |
|---|---|---|---|
| 0 | Angular (guards, `*hasPermission`) | Should the UI show this? | Hidden / redirect — **UX only, NOT security** |
| 1 | API authorization policy | Can this **role** do this **action** at all? | 403 before the handler runs |
| 2 | Handler data scope | ...on **this particular record**? | 403 or filtered results |

> **Golden rule:** the frontend never enforces security. Anyone can call the API with Postman — layers 1 and 2 must enforce every rule.

## 3. Permission matrix (permission + data scope)

| Permission | Admin | HR | Manager | Employee |
|---|---|---|---|---|
| `Employees.View` | ✅ all | ✅ all | ✅ own department | ✅ self only |
| `Employees.Create` / `Employees.Update` | ✅ | ✅ | ❌ | ❌ |
| `Employees.Terminate` | ✅ | ✅ | ❌ | ❌ |
| `Departments.View` | ✅ | ✅ | ✅ | ✅ |
| `Departments.Manage` | ✅ | ✅ | ❌ | ❌ |
| `Leave.Request` (own) | ✅ | ✅ | ✅ | ✅ |
| `Leave.Approve` | ✅ all | ✅ all | ✅ own dept, **never self** | ❌ |
| `Leave.ViewAll` | ✅ | ✅ | ❌ | ❌ |
| `Attendance.Record` (own) | ✅ | ✅ | ✅ | ✅ |
| `Attendance.View` | ✅ all | ✅ all | ✅ own department | ❌ |
| `Users.Manage` | ✅ | ❌ | ❌ | ❌ |

The API returns the current user's permission list from `GET /api/auth/me`; Angular uses it for layer 0.

**Accounts without an employee record** (the seeded `admin@hrm.local`) hold every permission but cannot request leave, check in or review leave: those actions are recorded against an employee (`ReviewedById`), so the API answers `403` with an explanation. HR does the reviewing.

**Out-of-scope records return 404, not 403**, so a caller cannot probe which employees or requests exist.

## 4. Implementation

### Permissions — defined once in the Application layer

```csharp
// HRM.Application/Common/Security/Permissions.cs
public static class Permissions
{
    public const string EmployeesView   = "Employees.View";
    public const string EmployeesCreate = "Employees.Create";
    public const string LeaveApprove    = "Leave.Approve";
    // ...
    public static readonly string[] All = [EmployeesView, EmployeesCreate, LeaveApprove /*, ...*/];
}

// HRM.Application/Common/Security/RolePermissions.cs
public static class RolePermissions
{
    private static readonly Dictionary<string, string[]> Map = new()
    {
        [Roles.Admin]    = Permissions.All,
        [Roles.HR]       = [Permissions.EmployeesView, Permissions.EmployeesCreate, Permissions.LeaveApprove /*, ...*/],
        [Roles.Manager]  = [Permissions.EmployeesView, Permissions.LeaveApprove /*, ...*/],
        [Roles.Employee] = [Permissions.EmployeesView /*, Leave.Request, Attendance.Record ...*/],
    };

    public static string[] For(string role) => Map.GetValueOrDefault(role, []);
    public static string[] RolesWith(string permission) =>
        Map.Where(kv => kv.Value.Contains(permission)).Select(kv => kv.Key).ToArray();
}
```

### Layer 1 — one policy per permission

```csharp
// HRM.API/Program.cs
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
        options.AddPolicy(permission, p => p.RequireRole(RolePermissions.RolesWith(permission)));
});

// Controller
[HttpPost("{id:guid}/approve")]
[Authorize(Policy = Permissions.LeaveApprove)]
public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
{
    await sender.Send(new ApproveLeaveRequestCommand(id), ct);
    return NoContent();
}
```

### Layer 2 — data scope in handlers

```csharp
// HRM.Application/Common/Security/DataScope.cs
public static IQueryable<Employee> ApplyScope(this IQueryable<Employee> q, ICurrentUser user) => user.Role switch
{
    Roles.Admin or Roles.HR => q,
    Roles.Manager           => q.Where(e => e.Department.ManagerId == user.EmployeeId),
    _                       => q.Where(e => e.Id == user.EmployeeId),
};
```

### Layer 0 — Angular guard & directive

```ts
// permission.guard.ts
export const permissionGuard = (permission: string): CanActivateFn => () =>
  inject(AuthService).hasPermission(permission) || inject(Router).createUrlTree(['/forbidden']);

// routes
{ path: 'leave/approvals', canActivate: [authGuard, permissionGuard('Leave.Approve')],
  loadComponent: () => import('./features/leave/approvals.component') }

// has-permission.directive.ts
@Directive({ selector: '[hasPermission]' })
export class HasPermissionDirective {
  private auth = inject(AuthService);
  private tpl = inject(TemplateRef<unknown>);
  private vcr = inject(ViewContainerRef);
  hasPermission = input.required<string>();

  constructor() {
    effect(() => {
      this.vcr.clear();
      if (this.auth.hasPermission(this.hasPermission())) this.vcr.createEmbeddedView(this.tpl);
    });
  }
}
// <button *hasPermission="'Employees.Create'">Add employee</button>
```
