using System.Net;
using HRM.API.IntegrationTests.Infrastructure;

namespace HRM.API.IntegrationTests;

[Collection(ApiCollection.Name)]
public class LeaveAndAttendanceTests(ApiFactory api)
{
    [Fact]
    public async Task Department_manager_approves_and_the_balance_is_deducted()
    {
        var engineer = await api.ClientFor(Accounts.Engineer2);
        var before = await (await engineer.GetAsync("/api/leave-requests/balance")).ReadAs<Balance>();
        var requestId = await Submit(engineer, "Annual");

        var approve = await (await api.ClientFor(Accounts.EngineeringManager)).PostJson($"/api/leave-requests/{requestId}/approve");

        await approve.ShouldHaveStatus(HttpStatusCode.NoContent);
        var after = await (await engineer.GetAsync("/api/leave-requests/balance")).ReadAs<Balance>();
        after.AnnualLeaveBalance.ShouldBe(before.AnnualLeaveBalance - 1);
    }

    [Fact]
    public async Task Manager_of_another_department_cannot_see_the_request()
    {
        var requestId = await Submit(await api.ClientFor(Accounts.Engineer2), "Sick");

        var approve = await (await api.ClientFor(Accounts.SalesManager)).PostJson($"/api/leave-requests/{requestId}/approve");

        await approve.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Employees_cannot_approve() =>
        await (await (await api.ClientFor(Accounts.Engineer)).GetAsync("/api/leave-requests/pending"))
            .ShouldHaveStatus(HttpStatusCode.Forbidden);

    [Fact]
    public async Task Nobody_approves_their_own_request()
    {
        var manager = await api.ClientFor(Accounts.SalesManager);
        var requestId = await Submit(manager, "Sick");

        await (await manager.PostJson($"/api/leave-requests/{requestId}/approve")).ShouldHaveStatus(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Admin_without_employee_record_cannot_review()
    {
        var requestId = await Submit(await api.ClientFor(Accounts.Engineer2), "Sick");

        await (await (await api.ClientFor(Accounts.Admin)).PostJson($"/api/leave-requests/{requestId}/approve"))
            .ShouldHaveStatus(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rejection_requires_a_note()
    {
        var requestId = await Submit(await api.ClientFor(Accounts.Engineer2), "Sick");
        var hr = await api.ClientFor(Accounts.Hr);

        await (await hr.PostJson($"/api/leave-requests/{requestId}/reject", new { note = "" })).ShouldHaveStatus(HttpStatusCode.BadRequest);
        await (await hr.PostJson($"/api/leave-requests/{requestId}/reject", new { note = "Busy week" })).ShouldHaveStatus(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task One_check_in_per_day()
    {
        var engineer = await api.ClientFor(Accounts.Engineer3);

        await (await engineer.PostJson("/api/attendance/check-in")).ShouldHaveStatus(HttpStatusCode.OK);
        await (await engineer.PostJson("/api/attendance/check-in")).ShouldHaveStatus(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(Accounts.Engineer, HttpStatusCode.Forbidden)]
    [InlineData(Accounts.EngineeringManager, HttpStatusCode.OK)]
    [InlineData(Accounts.Hr, HttpStatusCode.OK)]
    public async Task Team_attendance_is_for_managers_and_HR(string caller, HttpStatusCode expected) =>
        await (await (await api.ClientFor(caller)).GetAsync("/api/attendance")).ShouldHaveStatus(expected);

    private static async Task<Guid> Submit(HttpClient client, string type)
    {
        var day = Dates.NextFreeWeekday();
        var response = await client.PostJson("/api/leave-requests", new { type, startDate = day, endDate = day, reason = "Integration test" });
        await response.ShouldHaveStatus(HttpStatusCode.Created);
        return (await response.ReadAs<IdResponse>()).Id;
    }
}
