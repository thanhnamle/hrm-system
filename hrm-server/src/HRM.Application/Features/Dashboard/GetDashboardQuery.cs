using HRM.Application.Features.LeaveRequests;
using MediatR;

namespace HRM.Application.Features.Dashboard;

/// <summary>One call for the home page. Sections the caller's role doesn't get are null.</summary>
public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed record DashboardDto(
    string Role,
    DateOnly Today,
    OrganizationSummary? Organization, // Admin, HR
    TeamSummary? Team,                 // Manager
    PersonalSummary? Personal);        // anyone with an employee record

public sealed record OrganizationSummary(
    int Headcount,
    int OnLeaveToday,
    int PendingApprovals,
    IReadOnlyList<DepartmentHeadcount> HeadcountByDepartment);

public sealed record DepartmentHeadcount(Guid DepartmentId, string DepartmentName, int Headcount);

public sealed record TeamSummary(int TeamSize, int PresentToday, int OnLeaveToday, int PendingApprovals);

public sealed record PersonalSummary(
    decimal AnnualLeaveBalance,
    decimal PendingAnnualDays,
    int PendingRequests,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CheckedOutAt,
    IReadOnlyList<LeaveRequestDto> RecentRequests);
