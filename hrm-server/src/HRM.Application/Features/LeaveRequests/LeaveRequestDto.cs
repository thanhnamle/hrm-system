using HRM.Domain.Entities;
using HRM.Domain.Enums;

namespace HRM.Application.Features.LeaveRequests;

public sealed record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string DepartmentName,
    LeaveType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Days,
    string Reason,
    LeaveStatus Status,
    string? ReviewedByName,
    DateTimeOffset? ReviewedAt,
    string? ReviewNote,
    DateTimeOffset CreatedAt);

public sealed record LeaveBalanceDto(decimal AnnualLeaveBalance, decimal PendingAnnualDays, decimal Available);

internal static class LeaveRequestProjection
{
    /// <summary>
    /// Projects straight to the DTO in SQL. The reviewer has no navigation property,
    /// so their name comes from a subquery on <paramref name="employees"/>.
    /// </summary>
    public static IQueryable<LeaveRequestDto> SelectDto(this IQueryable<LeaveRequest> requests, IQueryable<Employee> employees) =>
        requests.Select(r => new LeaveRequestDto(
            r.Id,
            r.EmployeeId,
            r.Employee.FirstName + " " + r.Employee.LastName,
            r.Employee.Department.Name,
            r.Type,
            r.StartDate,
            r.EndDate,
            r.Days,
            r.Reason,
            r.Status,
            employees.Where(e => e.Id == r.ReviewedById).Select(e => e.FirstName + " " + e.LastName).FirstOrDefault(),
            r.ReviewedAt,
            r.ReviewNote,
            r.CreatedAt));
}
