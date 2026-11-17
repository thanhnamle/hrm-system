using System.Net;
using HRM.API.IntegrationTests.Infrastructure;

namespace HRM.API.IntegrationTests;

[Collection(ApiCollection.Name)]
public class DashboardTests(ApiFactory api)
{
    [Fact]
    public async Task Admin_gets_organization_data_quality_and_system_but_no_personal_section()
    {
        var dashboard = await Get(Accounts.Admin);

        dashboard.Organization.ShouldNotBeNull();
        dashboard.DataQuality.ShouldNotBeNull();
        dashboard.System.ShouldNotBeNull();
        dashboard.Team.ShouldBeNull();
        dashboard.Personal.ShouldBeNull(); // no employee record
    }

    [Fact]
    public async Task Organization_numbers_add_up()
    {
        var org = (await Get(Accounts.Hr)).Organization!;

        var a = org.AttendanceToday;
        (a.CheckedOut + a.Working + a.OnLeave + a.NotCheckedIn).ShouldBe(org.Headcount);
        org.HeadcountByDepartment.Sum(d => d.Headcount).ShouldBe(org.Headcount);
        org.LeaveDaysThisYear.Select(l => l.Type).ShouldBe(["Annual", "Sick", "Unpaid"]);
        org.PendingRequests.Count.ShouldBeLessThanOrEqualTo(5);
        org.RecentActivity.ShouldNotBeEmpty(); // every seeded employee has a Hired entry
        org.RecentActivity.ShouldBe(org.RecentActivity.OrderByDescending(x => x.EffectiveDate).ToList());
    }

    [Fact]
    public async Task Trends_cover_the_expected_periods()
    {
        var dashboard = await Get(Accounts.Hr);
        var org = dashboard.Organization!;

        // Working days of the last 30 days, oldest first, ending today when today is a weekday.
        org.AttendanceTrend.ShouldAllBe(d => d.Date.DayOfWeek != DayOfWeek.Saturday && d.Date.DayOfWeek != DayOfWeek.Sunday);
        org.AttendanceTrend.ShouldBe(org.AttendanceTrend.OrderBy(d => d.Date).ToList());
        org.AttendanceTrend.Count.ShouldBeInRange(20, 23);
        org.AttendanceTrend.ShouldAllBe(d => d.Present <= d.Expected + 1 && d.Expected > 0);

        org.HeadcountTrend.Count.ShouldBe(12);
        org.HeadcountTrend[^1].Month.ShouldBe(new DateOnly(dashboard.Today.Year, dashboard.Today.Month, 1));
        org.HeadcountTrend[^1].Headcount.ShouldBeGreaterThan(0);

        org.LeaveByMonth.Count.ShouldBe(9); // 6 months back, this month, 2 ahead

        var team = (await Get(Accounts.EngineeringManager)).Team!;
        team.AttendanceTrend.ShouldNotBeEmpty();
        team.AttendanceTrend.ShouldAllBe(d => d.Expected <= org.AttendanceTrend.First(o => o.Date == d.Date).Expected);

        var weeks = (await Get(Accounts.SalesPerson)).Personal!.HoursByWeek;
        weeks.Count.ShouldBe(8);
        weeks.ShouldAllBe(w => w.WeekStart.DayOfWeek == DayOfWeek.Monday);
    }

    [Fact]
    public async Task System_status_reports_the_database_check()
    {
        var system = (await Get(Accounts.Admin)).System!;

        system.Status.ShouldBe("Healthy");
        system.Components.ShouldContain(c => c.Name == "postgresql" && c.Status == "Healthy");
        system.StartedAt.ShouldBeLessThanOrEqualTo(system.CheckedAt);
    }

    [Fact]
    public async Task Data_quality_counts_an_employee_without_login_cv_skills_or_education()
    {
        var before = (await Get(Accounts.Hr)).DataQuality!;

        var hr = await api.ClientFor(Accounts.Hr);
        await (await hr.PostJson("/api/employees", EmployeeAccessTests.NewEmployee(await EmployeeAccessTests.NewDepartment(hr), "bare")))
            .ShouldHaveStatus(HttpStatusCode.Created);

        var after = (await Get(Accounts.Hr)).DataQuality!;
        after.ActiveEmployees.ShouldBe(before.ActiveEmployees + 1);
        after.WithoutLogin.ShouldBe(before.WithoutLogin + 1);
        after.WithoutCv.ShouldBe(before.WithoutCv + 1);
        after.WithoutSkills.ShouldBe(before.WithoutSkills + 1);
        after.WithoutQualifications.ShouldBe(before.WithoutQualifications + 1);
        after.AccountsByRole.Select(r => r.Role).ShouldBe(["Admin", "HR", "Manager", "Employee"]);
        after.NeedsAttention.ShouldAllBe(g => g.Missing.Count > 0);
    }

    [Fact]
    public async Task Manager_sees_only_their_department_and_no_organization()
    {
        var dashboard = await Get(Accounts.EngineeringManager);

        dashboard.Organization.ShouldBeNull();
        dashboard.DataQuality.ShouldBeNull();
        dashboard.System.ShouldBeNull();
        var team = dashboard.Team.ShouldNotBeNull();
        team.DepartmentName.ShouldBe("Engineering");
        team.Members.Count.ShouldBe(team.TeamSize);
        string[] statuses = ["NotCheckedIn", "Working", "CheckedOut", "OnLeave"];
        team.Members.ShouldAllBe(m => statuses.Contains(m.Status));
        var a = team.AttendanceToday;
        (a.CheckedOut + a.Working + a.OnLeave + a.NotCheckedIn).ShouldBe(team.TeamSize);
        team.PendingRequests.ShouldAllBe(r => r.DepartmentName == "Engineering");
    }

    [Fact]
    public async Task Employee_gets_only_their_own_section()
    {
        var dashboard = await Get(Accounts.SalesPerson);

        dashboard.Organization.ShouldBeNull();
        dashboard.Team.ShouldBeNull();
        dashboard.DataQuality.ShouldBeNull();
        dashboard.System.ShouldBeNull();
        var me = dashboard.Personal.ShouldNotBeNull();
        me.Profile.EmployeeCode.ShouldStartWith("EMP-");
        me.Profile.DepartmentName.ShouldBe("Sales");
        me.Profile.LastChange.ShouldNotBeNull().ChangeType.ShouldBe("Hired");
        me.LeaveDaysThisYear.Count.ShouldBe(3);
        me.AttendanceThisMonth.WorkingDaysSoFar.ShouldBeInRange(0, 23); // Mon–Fri up to today: at most 23 in any month
        me.AttendanceThisMonth.HoursWorked.ShouldBeGreaterThanOrEqualTo(0);
    }

    private async Task<DashboardResponse> Get(string account)
    {
        var response = await (await api.ClientFor(account)).GetAsync("/api/dashboard");
        await response.ShouldHaveStatus(HttpStatusCode.OK);
        return await response.ReadAs<DashboardResponse>();
    }

    // Only the fields the tests read.
    private sealed record DashboardResponse(
        string Role, DateOnly Today, OrgResponse? Organization, TeamResponse? Team, PersonalResponse? Personal, QualityResponse? DataQuality, SystemResponse? System);

    private sealed record Breakdown(int CheckedOut, int Working, int OnLeave, int NotCheckedIn);
    private sealed record TypeDays(string Type, decimal Days);
    private sealed record Activity(string ChangeType, DateOnly EffectiveDate);
    private sealed record DepartmentCount(string DepartmentName, int Headcount);
    private sealed record Request(string DepartmentName);

    private sealed record OrgResponse(
        int Headcount, List<DepartmentCount> HeadcountByDepartment, Breakdown AttendanceToday,
        List<TypeDays> LeaveDaysThisYear, List<Request> PendingRequests, List<Activity> RecentActivity,
        List<Day> AttendanceTrend, List<MonthCount> HeadcountTrend, List<MonthLeave> LeaveByMonth);

    private sealed record Day(DateOnly Date, int Present, int OnLeave, int Expected);
    private sealed record MonthCount(DateOnly Month, int Headcount, int Hires, int Leavers);
    private sealed record MonthLeave(DateOnly Month, decimal Annual, decimal Sick, decimal Unpaid);
    private sealed record Week(DateOnly WeekStart, double Hours, int DaysPresent);

    private sealed record Member(string Status);
    private sealed record TeamResponse(
        int TeamSize, string DepartmentName, Breakdown AttendanceToday, List<Member> Members, List<Request> PendingRequests, List<Day> AttendanceTrend);

    private sealed record Profile(string EmployeeCode, string DepartmentName, Activity? LastChange);
    private sealed record Month(int DaysPresent, int WorkingDaysSoFar, double HoursWorked);
    private sealed record PersonalResponse(Profile Profile, List<TypeDays> LeaveDaysThisYear, Month AttendanceThisMonth, List<Week> HoursByWeek);

    private sealed record RoleCount(string Role, int Count);
    private sealed record Gap(List<string> Missing);
    private sealed record QualityResponse(
        int ActiveEmployees, int WithoutLogin, int WithoutCv, int WithoutSkills, int WithoutQualifications,
        List<RoleCount> AccountsByRole, List<Gap> NeedsAttention);

    private sealed record Component(string Name, string Status);
    private sealed record SystemResponse(string Status, List<Component> Components, DateTimeOffset StartedAt, DateTimeOffset CheckedAt);
}
