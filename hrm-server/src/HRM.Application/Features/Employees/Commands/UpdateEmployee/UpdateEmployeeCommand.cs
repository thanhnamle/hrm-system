using MediatR;

namespace HRM.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>
/// A changed email also changes the login email. A changed position or department is recorded in
/// the job history as of today; <see cref="ChangeNote"/> explains it there (e.g. "Promotion").
/// </summary>
public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Position,
    Guid DepartmentId,
    string? Phone,
    DateOnly? DateOfBirth,
    string? ChangeNote = null) : IRequest;
