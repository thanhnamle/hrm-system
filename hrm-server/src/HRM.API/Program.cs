using HRM.API.ExceptionHandling;
using HRM.Application;
using HRM.Infrastructure;
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

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        });
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseStatusCodePages(); // empty 4xx responses (e.g. unknown route) also get a ProblemDetails body
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();              // /openapi/v1.json
        app.MapScalarApiReference();   // /scalar
    }

    // No UseHttpsRedirection: HTTPS is terminated by Nginx in front of the API.
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

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
