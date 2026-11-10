using MediatR;

namespace HRM.Application.Features.Departments.Commands.CreateDepartment;

public sealed record CreateDepartmentCommand(string Code, string Name, string? Description) : IRequest<Guid>;
