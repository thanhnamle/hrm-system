using System.Net;
using HRM.API.IntegrationTests.Infrastructure;

namespace HRM.API.IntegrationTests;

/// <summary>Layer 1 (policy) and layer 2 (data scope) on the employee endpoints.</summary>
[Collection(ApiCollection.Name)]
public class EmployeeAccessTests(ApiFactory api)
{
    [Fact]
    public async Task Manager_sees_only_their_own_department()
    {
        var page = await (await (await api.ClientFor(Accounts.EngineeringManager)).GetAsync("/api/employees?status=Active&pageSize=100"))
            .ReadAs<Page<EmployeeItem>>();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(e => e.DepartmentName == "Engineering");
    }

    [Fact]
    public async Task Employee_sees_only_themselves()
    {
        var client = await api.ClientFor(Accounts.Engineer);
        var me = await (await client.GetAsync("/api/auth/me")).ReadAs<Me>();

        var page = await (await client.GetAsync("/api/employees")).ReadAs<Page<EmployeeItem>>();

        page.TotalCount.ShouldBe(1);
        page.Items.Single().Id.ShouldBe(me.EmployeeId!.Value);
    }

    [Theory]
    [InlineData(Accounts.EngineeringManager)]
    [InlineData(Accounts.Engineer)]
    public async Task Records_outside_the_scope_are_not_found(string caller)
    {
        var salesPersonId = await EmployeeIdOf(Accounts.SalesPerson);

        var response = await (await api.ClientFor(caller)).GetAsync($"/api/employees/{salesPersonId}");

        await response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(Accounts.EngineeringManager)]
    [InlineData(Accounts.Engineer)]
    public async Task Only_HR_and_Admin_can_create_employees(string caller)
    {
        var response = await (await api.ClientFor(caller)).PostJson("/api/employees", NewEmployee(Guid.NewGuid(), "denied"));

        await response.ShouldHaveStatus(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HR_creates_and_terminates_an_employee()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var departmentId = await NewDepartment(hr); // own department, so other tests' counts are unaffected

        var created = await hr.PostJson("/api/employees", NewEmployee(departmentId, "lifecycle"));
        await created.ShouldHaveStatus(HttpStatusCode.Created);
        var id = (await created.ReadAs<IdResponse>()).Id;

        await (await hr.PostJson($"/api/employees/{id}/terminate")).ShouldHaveStatus(HttpStatusCode.NoContent);
        await (await hr.PostJson($"/api/employees/{id}/terminate")).ShouldHaveStatus(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Duplicate_email_is_a_conflict()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var body = NewEmployee(await NewDepartment(hr), "duplicate");

        await (await hr.PostJson("/api/employees", body)).ShouldHaveStatus(HttpStatusCode.Created);
        await (await hr.PostJson("/api/employees", body)).ShouldHaveStatus(HttpStatusCode.Conflict);
    }

    private async Task<Guid> EmployeeIdOf(string email) =>
        (await (await (await api.ClientFor(email)).GetAsync("/api/auth/me")).ReadAs<Me>()).EmployeeId!.Value;

    internal static async Task<Guid> NewDepartment(HttpClient hr)
    {
        var code = "T" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var response = await hr.PostJson("/api/departments", new { code, name = "Test " + code });
        await response.ShouldHaveStatus(HttpStatusCode.Created);
        return (await response.ReadAs<IdResponse>()).Id;
    }

    internal static object NewEmployee(Guid departmentId, string tag) => new
    {
        firstName = "Test",
        lastName = tag,
        email = $"{tag}.{Guid.NewGuid():N}@hrm.local",
        position = "Tester",
        departmentId,
        hireDate = "2026-01-05",
    };
}
