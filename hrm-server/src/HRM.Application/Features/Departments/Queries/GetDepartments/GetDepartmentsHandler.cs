using HRM.Application.Common.Interfaces;
using HRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Departments.Queries.GetDepartments;

public sealed class GetDepartmentsHandler(IApplicationDbContext db)
    : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken) =>
        await db.Departments.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id, d.Code, d.Name, d.Description, d.ManagerId,
                d.Manager != null ? d.Manager.FirstName + " " + d.Manager.LastName : null,
                d.Employees.Count(e => e.Status == EmployeeStatus.Active)))
            .ToListAsync(cancellationToken);
}
