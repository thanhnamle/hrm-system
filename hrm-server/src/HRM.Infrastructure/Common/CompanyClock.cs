using HRM.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace HRM.Infrastructure.Common;

/// <summary>"Company" configuration section.</summary>
public sealed class CompanyOptions
{
    public const string SectionName = "Company";

    /// <summary>IANA id. Decides what "today" is for leave and attendance.</summary>
    public string TimeZone { get; init; } = "Asia/Ho_Chi_Minh";
}

public sealed class CompanyClock(TimeProvider timeProvider, IOptions<CompanyOptions> options) : IClock
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, _timeZone).DateTime);
}
