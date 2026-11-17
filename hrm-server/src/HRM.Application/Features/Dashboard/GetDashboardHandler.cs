using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Application.Features.LeaveRequests;
using HRM.Domain.Common;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Dashboard;

public sealed class GetDashboardHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    IIdentityService identityService,
    ISystemHealth systemHealth)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private const int ListSize = 5;
    private const int UpcomingListSize = 8;
    private const int UpcomingDays = 14;

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var today = clock.Today;
        var role = currentUser.Role ?? string.Empty;
        var isOrgRole = role is Roles.Admin or Roles.HR;

        // One DbContext, so the sections run one after another (EF Core allows one query at a time).
        var organization = isOrgRole ? await OrganizationAsync(today, cancellationToken) : null;
        var team = role is Roles.Manager ? await TeamAsync(today, cancellationToken) : null;
        var personal = currentUser.EmployeeId is { } employeeId ? await PersonalAsync(employeeId, today, cancellationToken) : null;
        var dataQuality = isOrgRole ? await DataQualityAsync(cancellationToken) : null;
        var system = isOrgRole ? await systemHealth.CheckAsync(cancellationToken) : null;

        var isWorkingDay = today.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
        return new DashboardDto(role, today, isWorkingDay, organization, team, personal, dataQuality, system);
    }

    // ---- Organization ------------------------------------------------------------------------

    private async Task<OrganizationSummary> OrganizationAsync(DateOnly today, CancellationToken ct)
    {
        var active = ActiveEmployees();
        var rows = await TodayRowsAsync(active, today, ct);

        var byDepartment = await db.Departments.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentHeadcount(
                d.Id,
                d.Name,
                d.Employees.Count(e => e.Status == EmployeeStatus.Active),
                d.Manager == null ? null : d.Manager.FirstName + " " + d.Manager.LastName))
            .ToListAsync(ct);

        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var yearStart = new DateOnly(today.Year, 1, 1);

        return new OrganizationSummary(
            rows.Count,
            rows.Count(r => r.OnLeave),
            await PendingApprovals().CountAsync(ct),
            byDepartment,
            await db.Employees.CountAsync(e => e.HireDate >= monthStart && e.HireDate <= today, ct),
            await db.JobHistory.CountAsync(h => h.ChangeType == JobChangeType.Terminated && h.EffectiveDate >= yearStart && h.EffectiveDate <= today, ct),
            Breakdown(rows),
            await LeaveDaysThisYearAsync(db.LeaveRequests, today, ct),
            await OldestPendingAsync(ct),
            await UpcomingLeaveAsync(active, today, ct),
            await ActivityAsync(db.JobHistory, ListSize + 1, ct),
            await DashboardTrends.AttendanceAsync(db, db.Employees, today, ct),
            await DashboardTrends.HeadcountAsync(db, db.Employees, today, ct),
            await DashboardTrends.LeaveByMonthAsync(db, db.Employees, today, ct));
    }

    // ---- Team ----------------------------------------------------------------------------------

    private async Task<TeamSummary> TeamAsync(DateOnly today, CancellationToken ct)
    {
        var team = ActiveEmployees().ApplyScope(currentUser);
        var rows = await TodayRowsAsync(team, today, ct);

        var departmentName = currentUser.EmployeeId is { } id
            ? await db.Employees.Where(e => e.Id == id).Select(e => e.Department.Name).SingleOrDefaultAsync(ct) ?? string.Empty
            : string.Empty;

        return new TeamSummary(
            rows.Count,
            rows.Count(r => r.CheckInAt is not null),
            rows.Count(r => r.OnLeave),
            await PendingApprovals().CountAsync(ct),
            departmentName,
            Breakdown(rows),
            rows.Select(r => new TeamMemberToday(r.Id, r.Name, r.Position, StatusOf(r), r.CheckInAt, r.CheckOutAt)).ToList(),
            await OldestPendingAsync(ct),
            await UpcomingLeaveAsync(team, today, ct),
            await DashboardTrends.AttendanceAsync(db, db.Employees.ApplyScope(currentUser), today, ct));
    }

    // ---- Personal ------------------------------------------------------------------------------

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
                e.EmployeeCode,
                e.Position,
                DepartmentName = e.Department.Name,
                e.HireDate,
                SkillsCount = e.Skills.Length,
                QualificationsCount = db.Qualifications.Count(q => q.EmployeeId == e.Id),
                HasCv = db.EmployeeDocuments.Any(d => d.EmployeeId == e.Id),
            })
            .SingleOrDefaultAsync(ct);
        if (me is null)
            return null;

        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var month = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.WorkDate >= monthStart && a.WorkDate <= today)
            .Select(a => new { a.WorkDate, a.CheckInAt, a.CheckOutAt })
            .ToListAsync(ct);
        var todayRecord = month.SingleOrDefault(a => a.WorkDate == today);
        var hours = month
            .Where(a => a.CheckOutAt is not null)
            .Sum(a => (a.CheckOutAt!.Value - a.CheckInAt).TotalHours);

        var mine = db.LeaveRequests.AsNoTracking().Where(r => r.EmployeeId == employeeId);

        var recent = await mine
            .OrderByDescending(r => r.CreatedAt)
            .Take(ListSize)
            .SelectDto(db.Employees)
            .ToListAsync(ct);

        var nextLeave = await mine
            .Where(r => r.Status == LeaveStatus.Approved && r.EndDate >= today)
            .OrderBy(r => r.StartDate)
            .SelectDto(db.Employees)
            .FirstOrDefaultAsync(ct);

        var lastChange = (await ActivityAsync(db.JobHistory.Where(h => h.EmployeeId == employeeId), 1, ct)).FirstOrDefault();

        return new PersonalSummary(
            me.AnnualLeaveBalance,
            me.PendingAnnualDays,
            me.PendingRequests,
            todayRecord?.CheckInAt,
            todayRecord?.CheckOutAt,
            recent,
            new MyProfile(me.EmployeeCode, me.Position, me.DepartmentName, me.HireDate,
                me.SkillsCount, me.QualificationsCount, me.HasCv, lastChange),
            await LeaveDaysThisYearAsync(mine, today, ct),
            nextLeave,
            new MonthAttendance(month.Count, WorkingDays.Count(monthStart, today), Math.Round(hours, 1)),
            await DashboardTrends.HoursByWeekAsync(db, employeeId, today, ct));
    }

    // ---- Data quality --------------------------------------------------------------------------

    private async Task<DataQualitySummary> DataQualityAsync(CancellationToken ct)
    {
        var users = await identityService.GetUsersAsync(ct);
        var withLogin = users.Where(u => u.EmployeeId is not null).Select(u => u.EmployeeId!.Value).ToHashSet();

        var people = await ActiveEmployees()
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new
            {
                e.Id,
                Name = e.FirstName + " " + e.LastName,
                HasSkills = e.Skills.Length > 0,
                HasCv = db.EmployeeDocuments.Any(d => d.EmployeeId == e.Id),
                HasQualifications = db.Qualifications.Any(q => q.EmployeeId == e.Id),
            })
            .ToListAsync(ct);

        var gaps = people
            .Select(p => new ProfileGap(p.Id, p.Name, Missing(withLogin.Contains(p.Id), p.HasCv, p.HasSkills, p.HasQualifications)))
            .ToList();

        return new DataQualitySummary(
            people.Count,
            people.Count(p => !withLogin.Contains(p.Id)),
            people.Count(p => !p.HasCv),
            people.Count(p => !p.HasSkills),
            people.Count(p => !p.HasQualifications),
            users.Count(u => u.IsLockedOut),
            Roles.All.Select(r => new RoleCount(r, users.Count(u => u.Role == r))).ToList(),
            gaps.Where(g => g.Missing.Count > 0).OrderByDescending(g => g.Missing.Count).Take(ListSize).ToList());
    }

    private static List<string> Missing(bool hasLogin, bool hasCv, bool hasSkills, bool hasQualifications)
    {
        var missing = new List<string>(4);
        if (!hasLogin) missing.Add("login");
        if (!hasCv) missing.Add("CV");
        if (!hasSkills) missing.Add("skills");
        if (!hasQualifications) missing.Add("education");
        return missing;
    }

    // ---- Shared queries ------------------------------------------------------------------------

    private IQueryable<Employee> ActiveEmployees() =>
        db.Employees.AsNoTracking().Where(e => e.Status == EmployeeStatus.Active);

    /// <summary>Same rule as the approvals inbox: in review scope and not the caller's own.</summary>
    private IQueryable<LeaveRequest> PendingApprovals()
    {
        var employeeId = currentUser.EmployeeId;
        return db.LeaveRequests.AsNoTracking()
            .ApplyReviewScope(currentUser)
            .Where(r => r.Status == LeaveStatus.Pending && r.EmployeeId != employeeId);
    }

    private Task<List<LeaveRequestDto>> OldestPendingAsync(CancellationToken ct) =>
        PendingApprovals().OrderBy(r => r.CreatedAt).Take(ListSize).SelectDto(db.Employees).ToListAsync(ct);

    private Task<List<LeaveRequestDto>> UpcomingLeaveAsync(IQueryable<Employee> employees, DateOnly today, CancellationToken ct)
    {
        var until = today.AddDays(UpcomingDays);
        return db.LeaveRequests.AsNoTracking()
            .Where(r => r.Status == LeaveStatus.Approved && r.EndDate >= today && r.StartDate <= until)
            .Where(r => employees.Any(e => e.Id == r.EmployeeId))
            .OrderBy(r => r.StartDate)
            .Take(UpcomingListSize)
            .SelectDto(db.Employees)
            .ToListAsync(ct);
    }

    private static async Task<IReadOnlyList<LeaveTypeDays>> LeaveDaysThisYearAsync(
        IQueryable<LeaveRequest> requests, DateOnly today, CancellationToken ct)
    {
        var yearStart = new DateOnly(today.Year, 1, 1);
        var yearEnd = new DateOnly(today.Year, 12, 31);
        var days = await requests
            .Where(r => r.Status == LeaveStatus.Approved && r.StartDate >= yearStart && r.StartDate <= yearEnd)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, Days = g.Sum(r => r.Days) })
            .ToDictionaryAsync(x => x.Type, x => x.Days, ct);

        return Enum.GetValues<LeaveType>().Select(t => new LeaveTypeDays(t, days.GetValueOrDefault(t))).ToList();
    }

    /// <summary>Newest first; Guid v7 ids break ties between changes on the same day.</summary>
    private Task<List<ActivityItem>> ActivityAsync(IQueryable<JobHistoryEntry> history, int count, CancellationToken ct) =>
        (from h in history.AsNoTracking()
         join e in db.Employees on h.EmployeeId equals e.Id
         join d in db.Departments on h.DepartmentId equals d.Id
         orderby h.EffectiveDate descending, h.Id descending
         select new ActivityItem(h.EmployeeId, e.FirstName + " " + e.LastName, h.ChangeType, h.EffectiveDate, h.Position, d.Name, h.Note))
        .Take(count)
        .ToListAsync(ct);

    private sealed record TodayRow(
        Guid Id, string Name, string Position, DateTimeOffset? CheckInAt, DateTimeOffset? CheckOutAt, bool OnLeave);

    private Task<List<TodayRow>> TodayRowsAsync(IQueryable<Employee> employees, DateOnly today, CancellationToken ct) =>
        employees
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new TodayRow(
                e.Id,
                e.FirstName + " " + e.LastName,
                e.Position,
                db.AttendanceRecords.Where(a => a.EmployeeId == e.Id && a.WorkDate == today).Select(a => (DateTimeOffset?)a.CheckInAt).FirstOrDefault(),
                db.AttendanceRecords.Where(a => a.EmployeeId == e.Id && a.WorkDate == today).Select(a => a.CheckOutAt).FirstOrDefault(),
                e.LeaveRequests.Any(r => r.Status == LeaveStatus.Approved && r.StartDate <= today && r.EndDate >= today)))
            .ToListAsync(ct);

    /// <summary>Attendance wins over leave: someone on leave who checked in anyway is working.</summary>
    private static TodayStatus StatusOf(TodayRow row) => row switch
    {
        { CheckOutAt: not null } => TodayStatus.CheckedOut,
        { CheckInAt: not null } => TodayStatus.Working,
        { OnLeave: true } => TodayStatus.OnLeave,
        _ => TodayStatus.NotCheckedIn,
    };

    private static AttendanceBreakdown Breakdown(IReadOnlyCollection<TodayRow> rows)
    {
        var statuses = rows.Select(StatusOf).ToList();
        return new AttendanceBreakdown(
            statuses.Count(s => s == TodayStatus.CheckedOut),
            statuses.Count(s => s == TodayStatus.Working),
            statuses.Count(s => s == TodayStatus.OnLeave),
            statuses.Count(s => s == TodayStatus.NotCheckedIn));
    }
}
