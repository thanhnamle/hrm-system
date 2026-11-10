using MediatR;

namespace HRM.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>A changed email also changes the login email; a changed department is a transfer.</summary>
public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Position,
    Guid DepartmentId,
    string? Phone,
    DateOnly? DateOfBirth) : IRequest;
