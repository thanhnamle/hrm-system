using System.Diagnostics;
using System.Reflection;
using HRM.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HRM.Infrastructure.Common;

/// <summary>The same checks as GET /health, with per-check timings for the admin dashboard.</summary>
public sealed class SystemHealth(HealthCheckService healthChecks, TimeProvider timeProvider) : ISystemHealth
{
    private static readonly DateTimeOffset StartedAt = new(Process.GetCurrentProcess().StartTime.ToUniversalTime(), TimeSpan.Zero);

    private static readonly string Version =
        typeof(SystemHealth).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";

    public async Task<SystemStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);

        var components = report.Entries
            .Select(e => new ComponentStatus(e.Key, e.Value.Status.ToString(), Math.Round(e.Value.Duration.TotalMilliseconds, 1)))
            .ToList();

        return new SystemStatus(report.Status.ToString(), components, Version, StartedAt, timeProvider.GetUtcNow());
    }
}
