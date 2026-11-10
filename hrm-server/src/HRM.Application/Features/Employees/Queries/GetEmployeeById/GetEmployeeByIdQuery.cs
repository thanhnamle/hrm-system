using MediatR;

namespace HRM.Application.Features.Employees.Queries.GetEmployeeById;

public sealed record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeDetailDto>;
