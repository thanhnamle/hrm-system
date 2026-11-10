using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Application.Features.LeaveRequests;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Dashboard;

public sealed class GetDashboardHandler(IApplicationDbContext db, ICurrentUser currentUser, IClock clock)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private const int RecentRequestCount = 5;

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var today = clock.Today;
        var role = currentUser.Role ?? string.Empty;

        var organization = role is Roles.Admin or Roles.HR ? await OrganizationAsync(today, cancellationToken) : null;
        var team = role is Roles.Manager ? await TeamAsync(today, cancellationToken) : null;
        var personal = currentUser.EmployeeId is { } employeeId ? await PersonalAsync(employeeId, today, cancellationToken) : null;

        return new DashboardDto(role, today, organization, team, personal);
    }

    private async Task<OrganizationSummary> OrganizationAsync(DateOnly today, CancellationToken ct)
    {
        var active = db.Employees.AsNoTracking().Where(e => e.Status == EmployeeStatus.Active);

        var byDepartment = await db.Departments.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentHeadcount(d.Id, d.Name, d.Employees.Count(e => e.Status == EmployeeStatus.Active)))
            .ToListAsync(ct);

        return new OrganizationSummary(
            await active.CountAsync(ct),
            await OnLeave(active, today).CountAsync(ct),
            await PendingApprovals().CountAsync(ct),
            byDepartment);
    }

    private async Task<TeamSummary> TeamAsync(DateOnly today, CancellationToken ct)
    {
        var team = db.Employees.AsNoTracking().ApplyScope(currentUser).Where(e => e.Status == EmployeeStatus.Active);

        return new TeamSummary(
            await team.CountAsync(ct),
            await db.AttendanceRecords.CountAsync(a => a.WorkDate == today && team.Any(e => e.Id == a.EmployeeId), ct),
            await OnLeave(team, today).CountAsync(ct),
            await PendingApprovals().CountAsync(ct));
    }

    private async Task<PersonalSummary?> PersonalAsync(Guid employeeId, DateOnly today, CancellationToken ct)
    {
        var me = await db.Employees.AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new
            {
                e.AnnualLeaveBalance,
                PendingAnnualDays = e.LeaveRequests
                    .Where(r => r.Type == LeaveType.Annual && r.Status == LeaveStatus.Pending)
                    .Sum(r => r.Days),
                PendingRequests = e.LeaveRequests.Count(r => r.Status == LeaveStatus.Pending),
            })
            .SingleOrDefaultAsync(ct);
        if (me is null)
            return null;

        var attendance = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.WorkDate == today)
            .Select(a => new { a.CheckInAt, a.CheckOutAt })
            .SingleOrDefaultAsync(ct);

        var recent = await db.LeaveRequests.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(RecentRequestCount)
            .SelectDto(db.Employees)
            .ToListAsync(ct);

        return new PersonalSummary(
            me.AnnualLeaveBalance, me.PendingAnnualDays, me.PendingRequests,
            attendance?.CheckInAt, attendance?.CheckOutAt, recent);
    }

    /// <summary>Same rule as the approvals inbox: in review scope and not the caller's own.</summary>
    private IQueryable<LeaveRequest> PendingApprovals()
    {
        var employeeId = currentUser.EmployeeId;
        return db.LeaveRequests.AsNoTracking()
            .ApplyReviewScope(currentUser)
            .Where(r => r.Status == LeaveStatus.Pending && r.EmployeeId != employeeId);
    }

    private static IQueryable<Employee> OnLeave(IQueryable<Employee> employees, DateOnly today) =>
        employees.Where(e => e.LeaveRequests.Any(r =>
            r.Status == LeaveStatus.Approved && r.StartDate <= today && r.EndDate >= today));
}
