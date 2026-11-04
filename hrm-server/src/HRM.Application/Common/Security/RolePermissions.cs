namespace HRM.Application.Common.Security;

/// <summary>
/// The permission matrix from docs/rbac.md §3. It answers "can this role do this action at all";
/// which records the action applies to (own department, self) is the handler's data scope.
/// </summary>
public static class RolePermissions
{
    private static readonly Dictionary<string, IReadOnlyList<string>> Map = new()
    {
        [Roles.Admin] = Permissions.All,
        [Roles.HR] = Permissions.All.Where(p => p != Permissions.UsersManage).ToArray(),
        [Roles.Manager] =
        [
            Permissions.EmployeesView, Permissions.DepartmentsView,
            Permissions.LeaveRequest, Permissions.LeaveApprove,
            Permissions.AttendanceRecord, Permissions.AttendanceView,
        ],
        [Roles.Employee] =
        [
            Permissions.EmployeesView, Permissions.DepartmentsView,
            Permissions.LeaveRequest, Permissions.AttendanceRecord,
        ],
    };

    public static IReadOnlyList<string> For(string role) => Map.GetValueOrDefault(role, []);

    public static string[] RolesWith(string permission) =>
        Map.Where(kv => kv.Value.Contains(permission)).Select(kv => kv.Key).ToArray();
}
