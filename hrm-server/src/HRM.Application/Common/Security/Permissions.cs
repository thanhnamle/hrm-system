namespace HRM.Application.Common.Security;

/// <summary>
/// Every action the API protects. Each one becomes an authorization policy of the same name (see docs/rbac.md).
/// </summary>
public static class Permissions
{
    public const string EmployeesView = "Employees.View";
    public const string EmployeesCreate = "Employees.Create";
    public const string EmployeesUpdate = "Employees.Update";
    public const string EmployeesTerminate = "Employees.Terminate";

    public const string DepartmentsView = "Departments.View";
    public const string DepartmentsManage = "Departments.Manage";

    public const string LeaveRequest = "Leave.Request";
    public const string LeaveApprove = "Leave.Approve";
    public const string LeaveViewAll = "Leave.ViewAll";

    public const string AttendanceRecord = "Attendance.Record";
    public const string AttendanceView = "Attendance.View";

    public const string UsersManage = "Users.Manage";

    public static readonly IReadOnlyList<string> All =
    [
        EmployeesView, EmployeesCreate, EmployeesUpdate, EmployeesTerminate,
        DepartmentsView, DepartmentsManage,
        LeaveRequest, LeaveApprove, LeaveViewAll,
        AttendanceRecord, AttendanceView,
        UsersManage,
    ];
}
