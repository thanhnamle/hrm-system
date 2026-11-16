using HRM.Domain.Common;
using HRM.Domain.Enums;

namespace HRM.Domain.Entities;

/// <summary>A CV line: a degree, a certificate, or a job held before joining the company.</summary>
public class Qualification : AuditableEntity
{
    public const int MaxTitleLength = 150;
    public const int MaxOrganizationLength = 150;
    public const int MaxDescriptionLength = 1000;

    private Qualification() { } // EF Core

    internal Qualification(Guid employeeId) => EmployeeId = employeeId;

    public Guid EmployeeId { get; private set; }
    public QualificationType Type { get; private set; }

    /// <summary>Degree, certificate name, or job title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>School, issuer, or previous employer.</summary>
    public string Organization { get; private set; } = string.Empty;

    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? Description { get; private set; }

    internal void Set(QualificationType type, string title, string organization, DateOnly? startDate, DateOnly? endDate, string? description)
    {
        if (startDate is { } start && endDate is { } end && end < start)
            throw new DomainException("End date must be on or after the start date.");

        Type = type;
        Title = Guard.Required(title, "Title");
        Organization = Guard.Required(organization, "Organization");
        StartDate = startDate;
        EndDate = endDate;
        Description = Guard.Optional(description);
    }
}
