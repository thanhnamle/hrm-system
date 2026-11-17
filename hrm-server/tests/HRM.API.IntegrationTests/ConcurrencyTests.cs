using System.Net;
using HRM.API.IntegrationTests.Infrastructure;

namespace HRM.API.IntegrationTests;

/// <summary>
/// Requests that arrive at the same time. Each test fires its requests together (Task.WhenAll on the
/// in-memory server, which handles them in parallel) against a fresh employee of its own, so the
/// shared seed data is never touched. Without the row lock / sequence these fail most runs.
/// </summary>
[Collection(ApiCollection.Name)]
public class ConcurrencyTests(ApiFactory api)
{
    [Fact]
    public async Task Approving_several_requests_at_once_deducts_every_one()
    {
        var employee = (await api.NewEmployeeWithLoginAsync("approve-race")).Client;
        var before = await Balance(employee);
        var ids = new List<Guid>();
        for (var i = 0; i < 6; i++)
            ids.Add(await SubmitOneDay(employee, Dates.NextFreeWeekday()));

        // Each approval reads the balance and writes it back; without a lock they overwrite each other.
        var hr = await api.ClientFor(Accounts.Hr);
        var responses = await Task.WhenAll(ids.Select(id => hr.PostJson($"/api/leave-requests/{id}/approve")));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.NoContent);
        (await Balance(employee)).AnnualLeaveBalance.ShouldBe(before.AnnualLeaveBalance - 6);
    }

    [Fact]
    public async Task Submitting_the_same_leave_at_once_creates_it_only_once()
    {
        var employee = (await api.NewEmployeeWithLoginAsync("submit-race")).Client;
        var day = Dates.NextFreeWeekday();

        // A double-click: every request checks "no overlapping leave yet" before any of them is saved.
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            employee.PostJson("/api/leave-requests", new { type = "Annual", startDate = day, endDate = day, reason = "Double click" })));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity).ShouldBe(5); // overlap
        (await Balance(employee)).PendingAnnualDays.ShouldBe(1);
    }

    [Fact]
    public async Task Creating_employees_at_once_gives_each_a_unique_code()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var departmentId = await EmployeeAccessTests.NewDepartment(hr);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i =>
            hr.PostJson("/api/employees", EmployeeAccessTests.NewEmployee(departmentId, $"code-race-{i}"))));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created);
        var codes = new List<string>();
        foreach (var response in responses)
        {
            var id = (await response.ReadAs<IdResponse>()).Id;
            codes.Add((await (await hr.GetAsync($"/api/employees/{id}")).ReadAs<EmployeeDetails>()).EmployeeCode);
        }
        codes.Distinct().Count().ShouldBe(8);
        codes.ShouldAllBe(c => c.StartsWith("EMP-"));
    }

    private static async Task<Guid> SubmitOneDay(HttpClient client, DateOnly day)
    {
        var response = await client.PostJson("/api/leave-requests", new { type = "Annual", startDate = day, endDate = day, reason = "Race test" });
        await response.ShouldHaveStatus(HttpStatusCode.Created);
        return (await response.ReadAs<IdResponse>()).Id;
    }

    private static async Task<Balance> Balance(HttpClient client) =>
        await (await client.GetAsync("/api/leave-requests/balance")).ReadAs<Balance>();

    private sealed record EmployeeDetails(string EmployeeCode);
}
