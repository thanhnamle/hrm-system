using MediatR;

namespace HRM.Application.Features.Employees.Commands.TerminateEmployee;

public sealed record TerminateEmployeeCommand(Guid Id) : IRequest;
