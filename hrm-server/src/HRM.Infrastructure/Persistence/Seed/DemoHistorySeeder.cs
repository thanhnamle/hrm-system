using HRM.Domain.Common;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using HRM.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Infrastructure.Persistence.Seed;

/// <summary>
/// Demo data only: about three months of past attendance and six months of approved leave, so the
/// dashboard charts have a story to tell on a fresh install. Runs with the demo seed (Development or
/// Database:SeedDemoData), never in tests or a real deployment.
///
/// Everything goes through the domain (Employee.RequestLeave, LeaveRequest.Approve,
/// AttendanceRecord.CheckIn/CheckOut), so the generated history obeys the same rules as real use:
/// balances are deducted, overlapping leave is refused, reviewers never approve their own leave.
/// Idempotent: skipped when attendance older than a month already exists; never changes existing rows.
/// </summary>
public sealed class DemoHistorySeeder(
    AppDbContext db,
    TimeProvider timeProvider,
    IOptions<CompanyOptions> company,
    ILogger<DemoHistorySeeder> logger)
{
    private const int AttendanceDays = 90;
    private const int LeaveMonthsBack = 6;
    private const double AbsenceRate = 0.05;

    private static readonly string[] AnnualReasons = ["Family trip", "Holiday", "Personal matters", "Visiting parents", "Wedding"];
    private static readonly string[] SickReasons = ["Flu", "Doctor's appointment", "Dentist", "Not feeling well"];

    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(company.Value.TimeZone);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _zone).DateTime);

        if (await db.AttendanceRecords.AnyAsync(a => a.WorkDate < today.AddDays(-30), ct))
        {
            logger.LogInformation("Demo history already present, skipping");
            return;
        }

        var employees = await db.Employees
            .Include(e => e.LeaveRequests)
            .Include(e => e.Department)
            .Where(e => e.Status == EmployeeStatus.Active)
            .ToListAsync(ct);
        if (employees.Count == 0)
            return;

        // Fixed seed: every fresh install tells the same story.
        var random = new Random(2026);
        var leaveCount = SeedLeave(employees, today, random);
        await db.SaveChangesAsync(ct);

        var attendanceCount = await SeedAttendanceAsync(employees, today, random, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded demo history: {Leave} approved leave requests, {Attendance} attendance records",
            leaveCount, attendanceCount);
    }

    private int SeedLeave(List<Employee> employees, DateOnly today, Random random)
    {
        var hr = employees.Where(e => e.Department.Code == "HR").ToList();
        var count = 0;

        foreach (var employee in employees)
        {
            // The department manager reviews; a manager's own leave goes to someone in HR (never themselves).
            var reviewer = employee.Department.ManagerId is { } managerId && managerId != employee.Id
                ? managerId
                : hr.FirstOrDefault(h => h.Id != employee.Id)?.Id;
            if (reviewer is null)
                continue;

            // One or two past periods in the last six months, and sometimes one in the next two months.
            var periods = random.Next(1, 3);
            for (var i = 0; i < periods; i++)
                count += TryLeave(employee, reviewer.Value, RandomWeekday(random, today.AddMonths(-LeaveMonthsBack), today.AddDays(-3)), random) ? 1 : 0;
            if (random.NextDouble() < 0.4)
                count += TryLeave(employee, reviewer.Value, RandomWeekday(random, today.AddDays(7), today.AddMonths(2)), random) ? 1 : 0;
        }
        return count;
    }

    private bool TryLeave(Employee employee, Guid reviewerId, DateOnly start, Random random)
    {
        if (start < employee.HireDate)
            return false;

        var roll = random.NextDouble();
        var type = roll < 0.7 ? LeaveType.Annual : roll < 0.95 ? LeaveType.Sick : LeaveType.Unpaid;
        var length = type == LeaveType.Sick ? random.Next(1, 3) : random.Next(1, 4);
        var end = start;
        for (var added = 1; added < length;)
        {
            end = end.AddDays(1);
            if (end.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                added++;
        }
        var reason = type == LeaveType.Sick ? Pick(SickReasons, random) : type == LeaveType.Unpaid ? "Moving house" : Pick(AnnualReasons, random);

        try
        {
            // Requested two weeks before, approved two days later: the history looks like real use.
            var request = employee.RequestLeave(type, start, end, reason, today: start.AddDays(-14));
            request.Approve(reviewerId, At(start.AddDays(-12), 10, 0));
            // Added explicitly, like SubmitLeaveRequestHandler: the key is set client-side, so EF would
            // otherwise treat it as an existing row and the save would fail.
            db.LeaveRequests.Add(request);
            return true;
        }
        catch (DomainException)
        {
            return false; // overlaps another period or not enough balance: skip it
        }
    }

    private async Task<int> SeedAttendanceAsync(List<Employee> employees, DateOnly today, Random random, CancellationToken ct)
    {
        var from = today.AddDays(-AttendanceDays);
        var existing = (await db.AttendanceRecords
                .Where(a => a.WorkDate >= from)
                .Select(a => new { a.EmployeeId, a.WorkDate })
                .ToListAsync(ct))
            .Select(a => (a.EmployeeId, a.WorkDate))
            .ToHashSet();

        var count = 0;
        for (var day = from; day < today; day = day.AddDays(1))
        {
            if (WorkingDays.Count(day, day) == 0)
                continue;

            foreach (var employee in employees)
            {
                if (day < employee.HireDate || existing.Contains((employee.Id, day)))
                    continue;
                if (employee.LeaveRequests.Any(r => r.Status == LeaveStatus.Approved && r.StartDate <= day && r.EndDate >= day))
                    continue;
                if (random.NextDouble() < AbsenceRate)
                    continue;

                // In between 7:45 and 9:15, out after 7.5 to 9.5 hours.
                var checkIn = At(day, 7, 45).AddMinutes(random.Next(0, 91));
                var record = AttendanceRecord.CheckIn(employee, day, checkIn);
                record.CheckOut(checkIn.AddMinutes(450 + random.Next(0, 121)));
                db.AttendanceRecords.Add(record);
                count++;
            }
        }
        return count;
    }

    /// <summary>A local time on that day in the company's time zone, as an instant.</summary>
    private DateTimeOffset At(DateOnly day, int hour, int minute)
    {
        var local = day.ToDateTime(new TimeOnly(hour, minute));
        return new DateTimeOffset(local, _zone.GetUtcOffset(local)).ToUniversalTime();
    }

    private static DateOnly RandomWeekday(Random random, DateOnly from, DateOnly to)
    {
        var day = from.AddDays(random.Next(0, Math.Max(1, to.DayNumber - from.DayNumber)));
        while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day;
    }

    private static string Pick(string[] values, Random random) => values[random.Next(values.Length)];
}
