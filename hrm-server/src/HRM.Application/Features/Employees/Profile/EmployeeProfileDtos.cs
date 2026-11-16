using HRM.Domain.Enums;

namespace HRM.Application.Features.Employees.Profile;

/// <summary>Everything on the employee's profile tab, in one call.</summary>
public sealed record EmployeeProfileDto(
    Guid EmployeeId,
    AccountDto? Account,
    IReadOnlyList<string> Skills,
    IReadOnlyList<QualificationDto> Qualifications,
    IReadOnlyList<JobHistoryDto> JobHistory,
    CvInfoDto? Cv);

/// <summary>The login account, or null on the profile if the employee has none.</summary>
public sealed record AccountDto(string Role, bool IsLockedOut);

public sealed record QualificationDto(
    Guid Id,
    QualificationType Type,
    string Title,
    string Organization,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Description);

/// <summary>Oldest first. Position and department are the ones from that date on.</summary>
public sealed record JobHistoryDto(
    Guid Id,
    JobChangeType ChangeType,
    DateOnly EffectiveDate,
    string Position,
    Guid DepartmentId,
    string DepartmentName,
    string? Note);

public sealed record CvInfoDto(string FileName, int SizeBytes, DateTimeOffset UploadedAt);

public sealed record CvFileDto(string FileName, string ContentType, byte[] Content);
