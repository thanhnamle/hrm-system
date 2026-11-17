using HRM.Application.Common.Interfaces;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Dashboard;

/// <summary>
/// Time series for the dashboard charts. Each method takes the set of people in the caller's scope
/// (everyone, or a manager's department), including people who have since left, so past days and
/// months count who was employed then, not who is employed now.
/// </summary>
internal static class DashboardTrends
{
    public const int AttendanceDays = 30;
    public const int HeadcountMonths = 12;
    public const int LeaveMonthsBack = 6;
    public const int LeaveMonthsAhead = 2;
    public const int Weeks = 8;

    private sealed record Employment(Guid Id, DateOnly Hired, DateOnly? Left)
    {
        /// <summary>Employed on that day: hired by then and not yet left (the termination date is the first day gone).</summary>
        public bool IsEmployedOn(DateOnly day) => Hired <= day && (Left is null || Left > day);
    }

    private static Task<List<Employment>> EmploymentsAsync(IApplicationDbContext db, IQueryable<Employee> people, CancellationToken ct) =>
        people.AsNoTracking()
            .Select(e => new Employment(
                e.Id,
                e.HireDate,
                db.JobHistory
                    .Where(h => h.EmployeeId == e.Id && h.ChangeType == JobChangeType.Terminated)
                    .Max(h => (DateOnly?)h.EffectiveDate)))
            .ToListAsync(ct);

    /// <summary>Working days of the last <see cref="AttendanceDays"/> days, today included.</summary>
    public static async Task<IReadOnlyList<DailyAttendance>> AttendanceAsync(
        IApplicationDbContext db, IQueryable<Employee> people, DateOnly today, CancellationToken ct)
    {
        var from = today.AddDays(-(AttendanceDays - 1));
        var staff = await EmploymentsAsync(db, people, ct);
        var ids = people.Select(e => e.Id);

        var present = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.WorkDate >= from && a.WorkDate <= today && ids.Contains(a.EmployeeId))
            .GroupBy(a => a.WorkDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

        var leave = await db.LeaveRequests.AsNoTracking()
            .Where(r => r.Status == LeaveStatus.Approved && r.EndDate >= from && r.StartDate <= today && ids.Contains(r.EmployeeId))
            .Select(r => new { r.EmployeeId, r.StartDate, r.EndDate })
            .ToListAsync(ct);

        var days = new List<DailyAttendance>();
        for (var day = from; day <= today; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;
            var onLeave = leave.Where(r => r.StartDate <= day && r.EndDate >= day).Select(r => r.EmployeeId).Distinct().Count();
            days.Add(new DailyAttendance(day, present.GetValueOrDefault(day), onLeave, staff.Count(s => s.IsEmployedOn(day))));
        }
        return days;
    }

    /// <summary>The last <see cref="HeadcountMonths"/> months, the current one included.</summary>
    public static async Task<IReadOnlyList<MonthHeadcount>> HeadcountAsync(
        IApplicationDbContext db, IQueryable<Employee> people, DateOnly today, CancellationToken ct)
    {
        var staff = await EmploymentsAsync(db, people, ct);
        var first = MonthStart(today).AddMonths(-(HeadcountMonths - 1));

        return Enumerable.Range(0, HeadcountMonths)
            .Select(i =>
            {
                var month = first.AddMonths(i);
                var end = Min(month.AddMonths(1).AddDays(-1), today);
                return new MonthHeadcount(
                    month,
                    staff.Count(s => s.IsEmployedOn(end)),
                    staff.Count(s => s.Hired >= month && s.Hired <= end),
                    staff.Count(s => s.Left >= month && s.Left <= end));
            })
            .ToList();
    }

    /// <summary>Approved leave days by start month, from <see cref="LeaveMonthsBack"/> months ago to <see cref="LeaveMonthsAhead"/> ahead.</summary>
    public static async Task<IReadOnlyList<MonthLeave>> LeaveByMonthAsync(
        IApplicationDbContext db, IQueryable<Employee> people, DateOnly today, CancellationToken ct)
    {
        var first = MonthStart(today).AddMonths(-LeaveMonthsBack);
        var last = MonthStart(today).AddMonths(LeaveMonthsAhead + 1).AddDays(-1);
        var ids = people.Select(e => e.Id);

        var requests = await db.LeaveRequests.AsNoTracking()
            .Where(r => r.Status == LeaveStatus.Approved && r.StartDate >= first && r.StartDate <= last && ids.Contains(r.EmployeeId))
            .Select(r => new { r.Type, r.StartDate, r.Days })
            .ToListAsync(ct);

        return Enumerable.Range(0, LeaveMonthsBack + 1 + LeaveMonthsAhead)
            .Select(i =>
            {
                var month = first.AddMonths(i);
                var inMonth = requests.Where(r => MonthStart(r.StartDate) == month).ToList();
                decimal Sum(LeaveType type) => inMonth.Where(r => r.Type == type).Sum(r => r.Days);
                return new MonthLeave(month, Sum(LeaveType.Annual), Sum(LeaveType.Sick), Sum(LeaveType.Unpaid));
            })
            .ToList();
    }

    /// <summary>The last <see cref="Weeks"/> weeks (Monday to Sunday), the current one included.</summary>
    public static async Task<IReadOnlyList<WeekHours>> HoursByWeekAsync(
        IApplicationDbContext db, Guid employeeId, DateOnly today, CancellationToken ct)
    {
        var thisWeek = WeekStart(today);
        var from = thisWeek.AddDays(-7 * (Weeks - 1));

        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.WorkDate >= from && a.WorkDate <= today)
            .Select(a => new { a.WorkDate, a.CheckInAt, a.CheckOutAt })
            .ToListAsync(ct);

        return Enumerable.Range(0, Weeks)
            .Select(i =>
            {
                var week = from.AddDays(7 * i);
                var inWeek = records.Where(r => r.WorkDate >= week && r.WorkDate < week.AddDays(7)).ToList();
                var hours = inWeek.Where(r => r.CheckOutAt is not null).Sum(r => (r.CheckOutAt!.Value - r.CheckInAt).TotalHours);
                return new WeekHours(week, Math.Round(hours, 1), inWeek.Count);
            })
            .ToList();
    }

    public static DateOnly MonthStart(DateOnly date) => new(date.Year, date.Month, 1);

    public static DateOnly WeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
}
