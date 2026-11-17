using HRM.Application.Common.Interfaces;
using HRM.Application.Features.LeaveRequests;
using HRM.Domain.Enums;
using MediatR;

namespace HRM.Application.Features.Dashboard;

/// <summary>One call for the home page. Sections the caller's role doesn't get are null.</summary>
public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed record DashboardDto(
    string Role,
    DateOnly Today,
    bool IsWorkingDay,
    OrganizationSummary? Organization, // Admin, HR
    TeamSummary? Team,                 // Manager
    PersonalSummary? Personal,         // anyone with an employee record
    DataQualitySummary? DataQuality,   // Admin, HR
    SystemStatus? System);             // Admin, HR

// ---- Organization (Admin, HR) ----------------------------------------------------------------

public sealed record OrganizationSummary(
    int Headcount,
    int OnLeaveToday,
    int PendingApprovals,
    IReadOnlyList<DepartmentHeadcount> HeadcountByDepartment,
    int NewHiresThisMonth,
    int LeaversThisYear,
    AttendanceBreakdown AttendanceToday,
    IReadOnlyList<LeaveTypeDays> LeaveDaysThisYear,
    IReadOnlyList<LeaveRequestDto> PendingRequests,  // oldest first, the ones waiting longest
    IReadOnlyList<LeaveRequestDto> UpcomingLeave,    // approved, today and the next 14 days
    IReadOnlyList<ActivityItem> RecentActivity,      // latest job history entries
    IReadOnlyList<DailyAttendance> AttendanceTrend,  // working days of the last 30 days, oldest first
    IReadOnlyList<MonthHeadcount> HeadcountTrend,    // the last 12 months, oldest first
    IReadOnlyList<MonthLeave> LeaveByMonth);         // approved leave days, 6 months back to 2 ahead

public sealed record DepartmentHeadcount(Guid DepartmentId, string DepartmentName, int Headcount, string? ManagerName);

/// <summary>Active employees by today's state; the four numbers add up to the headcount.</summary>
public sealed record AttendanceBreakdown(int CheckedOut, int Working, int OnLeave, int NotCheckedIn);

/// <summary>Approved leave days starting this year. Every leave type is listed, 0 when unused.</summary>
public sealed record LeaveTypeDays(LeaveType Type, decimal Days);

/// <param name="Expected">People employed that day (hired, not yet left), so the rate is Present / Expected.</param>
public sealed record DailyAttendance(DateOnly Date, int Present, int OnLeave, int Expected);

/// <param name="Headcount">Employed at the end of the month (today for the current month).</param>
public sealed record MonthHeadcount(DateOnly Month, int Headcount, int Hires, int Leavers);

/// <summary>Approved leave days by the month the leave starts in.</summary>
public sealed record MonthLeave(DateOnly Month, decimal Annual, decimal Sick, decimal Unpaid);

public sealed record ActivityItem(
    Guid EmployeeId,
    string EmployeeName,
    JobChangeType ChangeType,
    DateOnly EffectiveDate,
    string Position,
    string DepartmentName,
    string? Note);

// ---- Team (Manager) --------------------------------------------------------------------------

public sealed record TeamSummary(
    int TeamSize,
    int PresentToday,
    int OnLeaveToday,
    int PendingApprovals,
    string DepartmentName,
    AttendanceBreakdown AttendanceToday,
    IReadOnlyList<TeamMemberToday> Members,
    IReadOnlyList<LeaveRequestDto> PendingRequests,
    IReadOnlyList<LeaveRequestDto> UpcomingLeave,
    IReadOnlyList<DailyAttendance> AttendanceTrend);

public enum TodayStatus
{
    NotCheckedIn,
    Working,
    CheckedOut,
    OnLeave,
}

public sealed record TeamMemberToday(
    Guid EmployeeId,
    string Name,
    string Position,
    TodayStatus Status,
    DateTimeOffset? CheckInAt,
    DateTimeOffset? CheckOutAt);

// ---- Personal (anyone with an employee record) ------------------------------------------------

public sealed record PersonalSummary(
    decimal AnnualLeaveBalance,
    decimal PendingAnnualDays,
    int PendingRequests,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CheckedOutAt,
    IReadOnlyList<LeaveRequestDto> RecentRequests,
    MyProfile Profile,
    IReadOnlyList<LeaveTypeDays> LeaveDaysThisYear,
    LeaveRequestDto? NextLeave,          // approved leave that hasn't ended yet, the soonest
    MonthAttendance AttendanceThisMonth,
    IReadOnlyList<WeekHours> HoursByWeek); // the last 8 weeks (Monday start), oldest first

/// <param name="Hours">Completed days only (checked in and out).</param>
public sealed record WeekHours(DateOnly WeekStart, double Hours, int DaysPresent);

public sealed record MyProfile(
    string EmployeeCode,
    string Position,
    string DepartmentName,
    DateOnly HireDate,
    int SkillsCount,
    int QualificationsCount,
    bool HasCv,
    ActivityItem? LastChange);

/// <param name="WorkingDaysSoFar">Monday–Friday from the 1st up to and including today.</param>
/// <param name="HoursWorked">Completed days only (checked in and out).</param>
public sealed record MonthAttendance(int DaysPresent, int WorkingDaysSoFar, double HoursWorked);

// ---- Data quality (Admin, HR) ------------------------------------------------------------------

public sealed record DataQualitySummary(
    int ActiveEmployees,
    int WithoutLogin,
    int WithoutCv,
    int WithoutSkills,
    int WithoutQualifications,
    int LockedAccounts,
    IReadOnlyList<RoleCount> AccountsByRole,
    IReadOnlyList<ProfileGap> NeedsAttention); // the active employees missing the most

public sealed record RoleCount(string Role, int Count);

/// <param name="Missing">Any of: login, CV, skills, education.</param>
public sealed record ProfileGap(Guid EmployeeId, string EmployeeName, IReadOnlyList<string> Missing);
