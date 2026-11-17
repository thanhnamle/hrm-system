using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HRM.API.IntegrationTests.Infrastructure;

/// <summary>Seeded demo accounts (see DbInitializer).</summary>
public static class Accounts
{
    public const string Admin = "admin@hrm.local";
    public const string Hr = "linh.tran@hrm.local";
    public const string Hr2 = "mai.pham@hrm.local";
    public const string EngineeringManager = "minh.nguyen@hrm.local";
    public const string SalesManager = "lan.bui@hrm.local";
    public const string Engineer = "huy.le@hrm.local";
    public const string Engineer2 = "khoa.dang@hrm.local";
    public const string Engineer3 = "tuan.phan@hrm.local";
    public const string SalesPerson = "quang.do@hrm.local";
}

public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}

/// <summary>A brand-new employee with a login, for tests that change or race on an account.</summary>
public sealed record TestAccount(HttpClient Client, string Email, Guid EmployeeId, Guid UserId);

public static class AccountFactory
{
    public const string Password = "Start#2026a";

    /// <summary>
    /// A new employee in a new department, with a login, signed in. Tests that lock, terminate or
    /// race on an account use one of these, so the shared seed accounts are never affected.
    /// </summary>
    public static async Task<TestAccount> NewEmployeeWithLoginAsync(this ApiFactory api, string tag, string role = "Employee")
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var body = EmployeeAccessTests.NewEmployee(await EmployeeAccessTests.NewDepartment(hr), tag);
        var email = (string)body.GetType().GetProperty("email")!.GetValue(body)!;
        var employeeId = (await (await hr.PostJson("/api/employees", body)).ReadAs<IdResponse>()).Id;

        var created = await (await api.ClientFor(Accounts.Admin)).PostJson("/api/users", new { employeeId, role, password = Password });
        await created.ShouldHaveStatus(HttpStatusCode.Created);
        var userId = (await created.ReadAs<IdResponse>()).Id;

        return new TestAccount(await api.FreshClientFor(email), email, employeeId, userId);
    }

    /// <summary>A client with a newly issued token (ClientFor caches one token per account).</summary>
    public static async Task<HttpClient> FreshClientFor(this ApiFactory api, string email, string password = Password)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await api.LoginAsync(email, password));
        return client;
    }
}

public static class HttpExtensions
{
    public static async Task<T> ReadAs<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json.Options))!;

    public static async Task ShouldHaveStatus(this HttpResponseMessage response, HttpStatusCode expected) =>
        response.StatusCode.ShouldBe(expected, await response.Content.ReadAsStringAsync());

    public static Task<HttpResponseMessage> PostJson(this HttpClient client, string url, object? body = null) =>
        client.PostAsJsonAsync(url, body, Json.Options);

    public static Task<HttpResponseMessage> PutJson(this HttpClient client, string url, object body) =>
        client.PutAsJsonAsync(url, body, Json.Options);
}

/// <summary>Distinct future weekdays, so leave requests from different tests never overlap.</summary>
public static class Dates
{
    private static int _offset = 30;

    public static DateOnly NextFreeWeekday()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(Interlocked.Add(ref _offset, 7));
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(1);
        return date;
    }
}

// Minimal response shapes; only the fields the tests read.
public sealed record Page<T>(List<T> Items, int TotalCount);
public sealed record IdResponse(Guid Id);
public sealed record Me(Guid Id, string Role, Guid? EmployeeId, List<string> Permissions);
public sealed record EmployeeItem(Guid Id, string FullName, string DepartmentName);
public sealed record Department(Guid Id, string Code);
public sealed record Balance(decimal AnnualLeaveBalance, decimal PendingAnnualDays, decimal Available);
