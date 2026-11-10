using System.Net;
using HRM.API.IntegrationTests.Infrastructure;

namespace HRM.API.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthTests(ApiFactory api)
{
    [Theory]
    [InlineData(Accounts.Admin, "Admin", 12)]
    [InlineData(Accounts.Hr, "HR", 11)]
    [InlineData(Accounts.EngineeringManager, "Manager", 6)]
    [InlineData(Accounts.Engineer, "Employee", 4)]
    public async Task Each_role_signs_in_with_its_permissions(string email, string role, int permissions)
    {
        var me = await (await (await api.ClientFor(email)).GetAsync("/api/auth/me")).ReadAs<Me>();

        me.Role.ShouldBe(role);
        me.Permissions.Count.ShouldBe(permissions);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_the_same_answer()
    {
        var client = api.CreateClient();

        var wrongPassword = await client.PostJson("/api/auth/login", new { email = Accounts.Hr2, password = "Wrong#123" });
        var unknownEmail = await client.PostJson("/api/auth/login", new { email = "nobody@hrm.local", password = "Wrong#123" });

        await wrongPassword.ShouldHaveStatus(HttpStatusCode.Unauthorized);
        (await unknownEmail.Content.ReadAsStringAsync()).ShouldContain("Invalid email or password.");
        (await wrongPassword.Content.ReadAsStringAsync()).ShouldContain("Invalid email or password.");
    }

    [Fact]
    public async Task Refresh_rotates_and_a_reused_token_revokes_every_session()
    {
        var client = api.CreateClient(new() { HandleCookies = false });
        var login = await client.PostJson("/api/auth/login", new { email = Accounts.SalesPerson, password = ApiFactory.Password });
        var first = RefreshCookie(login);

        var rotated = await Refresh(client, first);
        await rotated.ShouldHaveStatus(HttpStatusCode.OK);
        var second = RefreshCookie(rotated);
        second.ShouldNotBe(first);

        await (await Refresh(client, first)).ShouldHaveStatus(HttpStatusCode.Unauthorized);  // reuse = theft
        await (await Refresh(client, second)).ShouldHaveStatus(HttpStatusCode.Unauthorized); // so the newer one dies too
    }

    [Theory]
    [InlineData("/api/employees")]
    [InlineData("/api/users")]
    [InlineData("/api/dashboard")]
    public async Task Anonymous_callers_are_rejected(string url) =>
        await (await api.CreateClient().GetAsync(url)).ShouldHaveStatus(HttpStatusCode.Unauthorized);

    private static Task<HttpResponseMessage> Refresh(HttpClient client, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"hrm_refresh={token}");
        return client.SendAsync(request);
    }

    private static string RefreshCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("hrm_refresh=")).Split(';')[0]["hrm_refresh=".Length..];
}
