namespace HRM.Application.Common.Interfaces;

/// <summary>Runs the registered health checks (database, ...). Implemented in Infrastructure.</summary>
public interface ISystemHealth
{
    Task<SystemStatus> CheckAsync(CancellationToken cancellationToken = default);
}

/// <param name="Status">Healthy, Degraded or Unhealthy: the worst of the components.</param>
/// <param name="Version">Build version of the API (with the commit hash when built from git).</param>
public sealed record SystemStatus(
    string Status,
    IReadOnlyList<ComponentStatus> Components,
    string Version,
    DateTimeOffset StartedAt,
    DateTimeOffset CheckedAt);

/// <summary>One health check. No error text: it could reveal connection details.</summary>
public sealed record ComponentStatus(string Name, string Status, double DurationMs);
