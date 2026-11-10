namespace HRM.Application.Common.Interfaces;

/// <summary>
/// Current time. <see cref="Today"/> is the date in the company's time zone, which is what
/// leave start dates and attendance work dates are compared against.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}
