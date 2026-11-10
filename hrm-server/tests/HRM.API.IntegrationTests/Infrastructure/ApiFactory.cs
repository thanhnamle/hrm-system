using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using HRM.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HRM.API.IntegrationTests.Infrastructure;

/// <summary>
/// One PostgreSQL container and one in-memory API for the whole test run, seeded with the demo data.
/// Tests share the database, so each test only changes records it creates itself (or uses accounts
/// no other test touches) and never relies on counts other tests could change.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Password = "Test#Passw0rd";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17").Build();
    private readonly ConcurrentDictionary<string, string> _tokens = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing": no Development-only startup seeding, no user-secrets; everything comes from here.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
        builder.UseSetting("Jwt:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Seed:DefaultPassword", Password);
        builder.UseSetting("RateLimiting:LoginPermitLimit", "1000"); // tests log in far more than 5 times a minute
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.MigrateAsync();
        await initializer.SeedAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }

    /// <summary>An HttpClient signed in as <paramref name="email"/>. Tokens are cached per account.</summary>
    public async Task<HttpClient> ClientFor(string email, string password = Password)
    {
        var token = _tokens.TryGetValue(email, out var cached) ? cached : await LoginAsync(email, password);
        _tokens[email] = token;

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<string> LoginAsync(string email, string password = Password)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(Json.Options))!.AccessToken;
    }

    private sealed record LoginResponse(string AccessToken);
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
