using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Models;
using HRM.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Employees.Queries.GetEmployees;

public sealed class GetEmployeesHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeListItemDto>>
{
    public Task<PagedResult<EmployeeListItemDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Employees.AsNoTracking().ApplyScope(currentUser);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(e =>
                (e.FirstName + " " + e.LastName).ToLower().Contains(term) ||
                e.Email.Contains(term) || // stored lowercase
                e.EmployeeCode.ToLower().Contains(term));
        }

        if (request.DepartmentId is { } departmentId)
            query = query.Where(e => e.DepartmentId == departmentId);

        if (request.Status is { } status)
            query = query.Where(e => e.Status == status);

        return query
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new EmployeeListItemDto(
                e.Id, e.EmployeeCode, e.FirstName + " " + e.LastName, e.Email, e.Position,
                e.DepartmentId, e.Department.Name, e.Status, e.HireDate))
            .ToPagedResultAsync(request, cancellationToken);
    }
}
