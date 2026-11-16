using FluentValidation;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using MediatR;

namespace HRM.Application.Features.Employees.Profile;

/// <summary>Replaces the whole skill list.</summary>
public sealed record UpdateSkillsCommand(Guid EmployeeId, IReadOnlyList<string> Skills) : IRequest;

public sealed record AddQualificationCommand(
    Guid EmployeeId,
    QualificationType Type,
    string Title,
    string Organization,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Description) : IRequest<Guid>;

public sealed record UpdateQualificationCommand(
    Guid EmployeeId,
    Guid QualificationId,
    QualificationType Type,
    string Title,
    string Organization,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Description) : IRequest;

public sealed record DeleteQualificationCommand(Guid EmployeeId, Guid QualificationId) : IRequest;

/// <summary>PDF up to 5 MB; replaces the current CV. The API reads the upload into <see cref="Content"/>.</summary>
public sealed record UploadCvCommand(Guid EmployeeId, string FileName, byte[] Content) : IRequest<CvInfoDto>;

public sealed record DeleteCvCommand(Guid EmployeeId) : IRequest;

public sealed class UpdateSkillsValidator : AbstractValidator<UpdateSkillsCommand>
{
    public UpdateSkillsValidator()
    {
        RuleFor(x => x.Skills).NotNull()
            .Must(s => s.Count <= Employee.MaxSkills).WithMessage($"At most {Employee.MaxSkills} skills.");
        RuleForEach(x => x.Skills).NotNull().MaximumLength(Employee.MaxSkillLength);
    }
}

/// <summary>Shared rules for add and update; lengths match the database columns.</summary>
internal static class QualificationRules
{
    public static void Apply<T>(AbstractValidator<T> v,
        Func<T, QualificationType> type, Func<T, string> title, Func<T, string> organization,
        Func<T, DateOnly?> start, Func<T, DateOnly?> end, Func<T, string?> description)
    {
        v.RuleFor(x => type(x)).IsInEnum().OverridePropertyName("Type");
        v.RuleFor(x => title(x)).NotEmpty().MaximumLength(Qualification.MaxTitleLength).OverridePropertyName("Title");
        v.RuleFor(x => organization(x)).NotEmpty().MaximumLength(Qualification.MaxOrganizationLength).OverridePropertyName("Organization");
        v.RuleFor(x => description(x)).MaximumLength(Qualification.MaxDescriptionLength).OverridePropertyName("Description");
        v.RuleFor(x => end(x))
            .GreaterThanOrEqualTo(x => start(x)).When(x => start(x).HasValue && end(x).HasValue)
            .WithMessage("'End Date' must be on or after the start date.")
            .OverridePropertyName("EndDate");
    }
}

public sealed class AddQualificationValidator : AbstractValidator<AddQualificationCommand>
{
    public AddQualificationValidator() =>
        QualificationRules.Apply(this, x => x.Type, x => x.Title, x => x.Organization, x => x.StartDate, x => x.EndDate, x => x.Description);
}

public sealed class UpdateQualificationValidator : AbstractValidator<UpdateQualificationCommand>
{
    public UpdateQualificationValidator() =>
        QualificationRules.Apply(this, x => x.Type, x => x.Title, x => x.Organization, x => x.StartDate, x => x.EndDate, x => x.Description);
}

public sealed class UploadCvValidator : AbstractValidator<UploadCvCommand>
{
    public UploadCvValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Content).NotEmpty().WithMessage("Choose a file to upload.");
    }
}
