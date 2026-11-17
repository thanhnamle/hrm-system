using System.Text.Json.Serialization;
using HRM.API.ExceptionHandling;
using HRM.API.OpenApi;
using HRM.API.Security;
using HRM.Application;
using HRM.Infrastructure;
using HRM.Infrastructure.Persistence.Seed;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger catches errors that happen before configuration is loaded.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddPermissionPolicies();
    builder.Services.AddLoginRateLimiting(builder.Configuration);
    builder.Services.AddReverseProxySupport(builder.Configuration);

    // Enums travel as names ("Annual", "Pending"), which is what the Angular client and Postman send back.
    builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        });
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    var app = builder.Build();

    // Development always migrates and seeds. Elsewhere it is opt-in: the Docker demo turns both on,
    // a real production deploy would run migrations as a separate step and never seed demo data.
    var migrate = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup");
    var seed = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:SeedDemoData");
    if (migrate || seed)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        if (migrate)
            await initializer.MigrateAsync();
        if (seed)
        {
            await initializer.SeedAsync();
            // Past attendance and leave for the dashboard charts (demo only; skipped once present).
            await scope.ServiceProvider.GetRequiredService<DemoHistorySeeder>().SeedAsync();
        }
    }

    // First, so logging and the rate limiter see the real client IP (behind Nginx).
    app.UseReverseProxySupport();
    // Outermost, so it logs the final status code (e.g. 401) rather than the exception the handler below turns into it.
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages(); // empty 4xx responses (e.g. unknown route) also get a ProblemDetails body

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();              // /openapi/v1.json
        app.MapScalarApiReference().AllowAnonymous();   // /scalar
    }

    // No UseHttpsRedirection: HTTPS is terminated by Nginx in front of the API.
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) // HostAbortedException is expected when running `dotnet ef`
{
    Log.Fatal(ex, "HRM API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Makes Program visible to WebApplicationFactory in HRM.API.IntegrationTests.
public partial class Program;
