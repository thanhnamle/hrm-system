using System.Net;
using HRM.API.IntegrationTests.Infrastructure;

namespace HRM.API.IntegrationTests;

/// <summary>
/// An access token issued *before* an account change must stop working right after it. Logging in
/// again afterwards would prove nothing (login always checks the current state), so every test keeps
/// using the client that holds the old token.
/// </summary>
[Collection(ApiCollection.Name)]
public class SessionTests(ApiFactory api)
{
    [Fact]
    public async Task Terminating_the_employee_ends_their_session_at_once()
    {
        var account = await api.NewEmployeeWithLoginAsync("session-terminate");
        await (await account.Client.GetAsync("/api/auth/me")).ShouldHaveStatus(HttpStatusCode.OK);

        var hr = await api.ClientFor(Accounts.Hr);
        await (await hr.PostJson($"/api/employees/{account.EmployeeId}/terminate")).ShouldHaveStatus(HttpStatusCode.NoContent);

        await (await account.Client.GetAsync("/api/auth/me")).ShouldHaveStatus(HttpStatusCode.Unauthorized);
        await (await account.Client.GetAsync("/api/leave-requests/mine")).ShouldHaveStatus(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Locking_ends_the_session_and_an_unlock_does_not_revive_the_old_token()
    {
        var account = await api.NewEmployeeWithLoginAsync("session-lock");
        var admin = await api.ClientFor(Accounts.Admin);

        await (await admin.PostJson($"/api/users/{account.UserId}/lock")).ShouldHaveStatus(HttpStatusCode.NoContent);
        await (await account.Client.GetAsync("/api/auth/me")).ShouldHaveStatus(HttpStatusCode.Unauthorized);

        await (await admin.PostJson($"/api/users/{account.UserId}/unlock")).ShouldHaveStatus(HttpStatusCode.NoContent);
        await (await account.Client.GetAsync("/api/auth/me")).ShouldHaveStatus(HttpStatusCode.Unauthorized);

        // Signing in again after the unlock works as usual.
        var fresh = await api.FreshClientFor(account.Email);
        await (await fresh.GetAsync("/api/auth/me")).ShouldHaveStatus(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_role_change_ends_the_old_session_so_old_permissions_stop_working()
    {
        var account = await api.NewEmployeeWithLoginAsync("session-demote", role: "Manager");
        await (await account.Client.GetAsync("/api/leave-requests/pending")).ShouldHaveStatus(HttpStatusCode.OK);

        var admin = await api.ClientFor(Accounts.Admin);
        await (await admin.PutJson($"/api/users/{account.UserId}/role", new { role = "Employee" })).ShouldHaveStatus(HttpStatusCode.NoContent);

        // The old token still says "Manager"; it must not keep the manager's permissions.
        await (await account.Client.GetAsync("/api/leave-requests/pending")).ShouldHaveStatus(HttpStatusCode.Unauthorized);

        var fresh = await api.FreshClientFor(account.Email);
        (await (await fresh.GetAsync("/api/auth/me")).ReadAs<Me>()).Role.ShouldBe("Employee");
        await (await fresh.GetAsync("/api/leave-requests/pending")).ShouldHaveStatus(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Changing_your_password_ends_your_other_sessions()
    {
        var account = await api.NewEmployeeWithLoginAsync("session-password");
        var otherDevice = await api.FreshClientFor(account.Email);

        await (await account.Client.PostJson("/api/auth/change-password",
                new { currentPassword = AccountFactory.Password, newPassword = "Changed#2026b" }))
            .ShouldHaveStatus(HttpStatusCode.NoContent);

        await (await otherDevice.GetAsync("/api/auth/me")).ShouldHaveStatus(HttpStatusCode.Unauthorized);
        var fresh = await api.FreshClientFor(account.Email, "Changed#2026b");
        await (await fresh.GetAsync("/api/auth/me")).ShouldHaveStatus(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Other_accounts_are_not_affected()
    {
        var account = await api.NewEmployeeWithLoginAsync("session-bystander");
        var other = await api.NewEmployeeWithLoginAsync("session-target");

        await (await (await api.ClientFor(Accounts.Admin)).PostJson($"/api/users/{other.UserId}/lock")).ShouldHaveStatus(HttpStatusCode.NoContent);

        await (await account.Client.GetAsync("/api/auth/me")).ShouldHaveStatus(HttpStatusCode.OK);
    }
}
