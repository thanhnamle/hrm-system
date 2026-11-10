using System.Net;
using HRM.API.IntegrationTests.Infrastructure;

namespace HRM.API.IntegrationTests;

[Collection(ApiCollection.Name)]
public class UserManagementTests(ApiFactory api)
{
    [Theory]
    [InlineData(Accounts.Hr)]
    [InlineData(Accounts.EngineeringManager)]
    [InlineData(Accounts.Engineer)]
    public async Task Only_Admin_manages_accounts(string caller) =>
        await (await (await api.ClientFor(caller)).GetAsync("/api/users")).ShouldHaveStatus(HttpStatusCode.Forbidden);

    [Fact]
    public async Task Account_lifecycle_create_promote_lock_unlock()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var admin = await api.ClientFor(Accounts.Admin);
        var employee = EmployeeAccessTests.NewEmployee(await EmployeeAccessTests.NewDepartment(hr), "account");
        var email = (string)employee.GetType().GetProperty("email")!.GetValue(employee)!;
        var employeeId = (await (await hr.PostJson("/api/employees", employee)).ReadAs<IdResponse>()).Id;

        var created = await admin.PostJson("/api/users", new { employeeId, role = "Employee", password = "Start#2026a" });
        await created.ShouldHaveStatus(HttpStatusCode.Created);
        var userId = (await created.ReadAs<IdResponse>()).Id;
        await (await admin.PostJson("/api/users", new { employeeId, role = "Employee", password = "Start#2026a" }))
            .ShouldHaveStatus(HttpStatusCode.Conflict);

        await (await admin.PutJson($"/api/users/{userId}/role", new { role = "Manager" })).ShouldHaveStatus(HttpStatusCode.NoContent);
        var me = await (await (await api.ClientFor(email, "Start#2026a")).GetAsync("/api/auth/me")).ReadAs<Me>();
        me.Role.ShouldBe("Manager");

        await (await admin.PostJson($"/api/users/{userId}/lock")).ShouldHaveStatus(HttpStatusCode.NoContent);
        var lockedLogin = await api.CreateClient().PostJson("/api/auth/login", new { email, password = "Start#2026a" });
        await lockedLogin.ShouldHaveStatus(HttpStatusCode.Unauthorized);

        await (await admin.PostJson($"/api/users/{userId}/unlock")).ShouldHaveStatus(HttpStatusCode.NoContent);
        await (await api.CreateClient().PostJson("/api/auth/login", new { email, password = "Start#2026a" }))
            .ShouldHaveStatus(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Weak_password_is_rejected_by_the_policy()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var employeeId = (await (await hr.PostJson("/api/employees",
            EmployeeAccessTests.NewEmployee(await EmployeeAccessTests.NewDepartment(hr), "weak"))).ReadAs<IdResponse>()).Id;

        var response = await (await api.ClientFor(Accounts.Admin)).PostJson("/api/users", new { employeeId, role = "Employee", password = "password" });

        await response.ShouldHaveStatus(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_cannot_lock_themselves()
    {
        var admin = await api.ClientFor(Accounts.Admin);
        var me = await (await admin.GetAsync("/api/auth/me")).ReadAs<Me>();

        await (await admin.PostJson($"/api/users/{me.Id}/lock")).ShouldHaveStatus(HttpStatusCode.UnprocessableEntity);
    }
}
