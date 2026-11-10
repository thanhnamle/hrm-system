using MediatR;

namespace HRM.Application.Features.Departments.Commands.AssignManager;

/// <summary>
/// The manager approves the department's leave. Their account also needs the Manager role
/// (or HR/Admin) to use the approval endpoints; roles are managed separately.
/// </summary>
public sealed record AssignManagerCommand(Guid DepartmentId, Guid EmployeeId) : IRequest;

public sealed record RemoveManagerCommand(Guid DepartmentId) : IRequest;
