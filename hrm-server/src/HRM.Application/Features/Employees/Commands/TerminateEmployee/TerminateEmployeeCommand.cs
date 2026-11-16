using FluentValidation;
using MediatR;

namespace HRM.Application.Features.Employees.Commands.TerminateEmployee;

/// <summary>Effective today. <see cref="Note"/> (e.g. "Resigned") is kept in the job history.</summary>
public sealed record TerminateEmployeeCommand(Guid Id, string? Note = null) : IRequest;

public sealed class TerminateEmployeeValidator : AbstractValidator<TerminateEmployeeCommand>
{
    public TerminateEmployeeValidator() => RuleFor(x => x.Note).MaximumLength(500);
}
