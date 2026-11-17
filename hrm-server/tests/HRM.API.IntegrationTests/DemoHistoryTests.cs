using System.Net.Http.Json;
using HRM.API.IntegrationTests.Infrastructure;
using HRM.Infrastructure.Persistence;
using HRM.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.API.IntegrationTests;

/// <summary>
/// The demo history seeder changes leave balances and attendance, so it gets its own database
/// (its own ApiFactory) instead of the shared one the other tests rely on. It is still in the shared
/// collection so the two test servers never start at the same time: building two hosts in parallel
/// races in WebApplicationFactory ("The entry point exited without ever building an IHost").
/// </summary>
[Collection(ApiCollection.Name)]
public class DemoHistoryTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();

    public Task InitializeAsync() => _api.InitializeAsync();

    public Task DisposeAsync() => _api.DisposeAsync();

    [Fact]
    public async Task Fills_the_charts_through_the_domain_and_runs_only_once()
    {
        await SeedHistory();
        var (attendance, leave) = await Counts();

        attendance.ShouldBeGreaterThan(300); // ~10 people × ~60 working days, minus leave and absences
        leave.ShouldBeGreaterThan(5);

        var hr = await _api.ClientFor(Accounts.Hr);
        var dashboard = (await hr.GetFromJsonAsync<Dashboard>("/api/dashboard", Json.Options))!;
        var pastDays = dashboard.Organization.AttendanceTrend.Where(d => d.Date < dashboard.Today).ToList();
        pastDays.ShouldAllBe(d => d.Present + d.OnLeave <= d.Expected);
        pastDays.Average(d => (double)d.Present / d.Expected).ShouldBeInRange(0.75, 1.0);
        dashboard.Organization.LeaveByMonth.Sum(m => m.Annual + m.Sick + m.Unpaid).ShouldBeGreaterThan(0);

        await SeedHistory(); // second start: nothing added
        (await Counts()).ShouldBe((attendance, leave));
    }

    private async Task SeedHistory()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoHistorySeeder>().SeedAsync();
    }

    private async Task<(int Attendance, int Leave)> Counts()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.AttendanceRecords.CountAsync(), await db.LeaveRequests.CountAsync());
    }

    private sealed record Dashboard(DateOnly Today, Org Organization);
    private sealed record Org(List<Day> AttendanceTrend, List<MonthLeave> LeaveByMonth);
    private sealed record Day(DateOnly Date, int Present, int OnLeave, int Expected);
    private sealed record MonthLeave(decimal Annual, decimal Sick, decimal Unpaid);
}
