using MediatR;

namespace HRM.Application.Features.Departments.Commands.DeleteDepartment;

public sealed record DeleteDepartmentCommand(Guid Id) : IRequest;
