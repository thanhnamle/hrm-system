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
    builder.Services.AddLoginRateLimiting();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        });
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        // Apply pending migrations and insert demo data. Production runs migrations as a separate deploy step.
        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.MigrateAsync();
        await initializer.SeedAsync();
    }

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
