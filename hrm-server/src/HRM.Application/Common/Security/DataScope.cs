using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Entities;

namespace HRM.Application.Common.Security;

/// <summary>
/// Layer 2 of docs/rbac.md: which records a caller may see. The permission policy already decided
/// that the role may use the endpoint at all; this narrows it to the right rows.
/// Records outside the scope are simply not found (404), so their existence is not revealed.
/// </summary>
public static class DataScope
{
    /// <summary>Admin and HR: everyone. Manager: departments they manage. Employee: themselves.</summary>
    public static IQueryable<Employee> ApplyScope(this IQueryable<Employee> employees, ICurrentUser user)
    {
        var employeeId = user.EmployeeId;

        return user.Role switch
        {
            Roles.Admin or Roles.HR => employees,
            // Without an employee id, "ManagerId == null" would match every department without a manager.
            _ when employeeId is null => employees.Where(_ => false),
            Roles.Manager => employees.Where(e => e.Department.ManagerId == employeeId),
            Roles.Employee => employees.Where(e => e.Id == employeeId),
            _ => employees.Where(_ => false),
        };
    }

    /// <summary>Requests the caller may approve or reject: all for Admin/HR, own departments for a Manager.</summary>
    public static IQueryable<LeaveRequest> ApplyReviewScope(this IQueryable<LeaveRequest> requests, ICurrentUser user)
    {
        var employeeId = user.EmployeeId;

        return user.Role switch
        {
            Roles.Admin or Roles.HR => requests,
            Roles.Manager when employeeId is not null => requests.Where(r => r.Employee.Department.ManagerId == employeeId),
            _ => requests.Where(_ => false),
        };
    }

    /// <summary>
    /// The caller's own employee id. Accounts without an employee record (the system admin) cannot
    /// request leave, check in or review requests.
    /// </summary>
    public static Guid RequireEmployeeId(this ICurrentUser user) =>
        user.EmployeeId ?? throw new ForbiddenAccessException("Your account is not linked to an employee record.");
}
