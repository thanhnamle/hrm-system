using MediatR;

namespace HRM.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>The employee code (EMP-0001, ...) is generated. A login account is created separately.</summary>
public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    string Position,
    Guid DepartmentId,
    DateOnly HireDate,
    string? Phone,
    DateOnly? DateOfBirth) : IRequest<Guid>;
