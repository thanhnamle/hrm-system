using MediatR;

namespace HRM.Application.Features.Departments.Commands.UpdateDepartment;

/// <summary>The code is fixed once created; only name and description change.</summary>
public sealed record UpdateDepartmentCommand(Guid Id, string Name, string? Description) : IRequest;
