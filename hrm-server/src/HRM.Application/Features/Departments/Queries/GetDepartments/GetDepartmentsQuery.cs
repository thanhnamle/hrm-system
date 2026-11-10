using MediatR;

namespace HRM.Application.Features.Departments.Queries.GetDepartments;

public sealed record GetDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>;

/// <summary><see cref="EmployeeCount"/> counts active employees only.</summary>
public sealed record DepartmentDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid? ManagerId,
    string? ManagerName,
    int EmployeeCount);
