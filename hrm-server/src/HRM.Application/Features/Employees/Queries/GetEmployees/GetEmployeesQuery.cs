using HRM.Application.Common.Models;
using HRM.Domain.Enums;
using MediatR;

namespace HRM.Application.Features.Employees.Queries.GetEmployees;

/// <summary>Search matches name, email or employee code. Results are limited to the caller's data scope.</summary>
public sealed record GetEmployeesQuery : IRequest<PagedResult<EmployeeListItemDto>>, IPagedQuery
{
    public string? Search { get; init; }
    public Guid? DepartmentId { get; init; }
    public EmployeeStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
