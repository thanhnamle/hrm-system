using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeeByIdQuery, EmployeeDetailDto>
{
    public async Task<EmployeeDetailDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken) =>
        await db.Employees.AsNoTracking()
            .ApplyScope(currentUser)
            .Where(e => e.Id == request.Id)
            .Select(e => new EmployeeDetailDto(
                e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.FirstName + " " + e.LastName, e.Email,
                e.Phone, e.DateOfBirth, e.HireDate, e.Position, e.DepartmentId, e.Department.Name,
                e.Department.ManagerId == e.Id, e.Status, e.AnnualLeaveBalance, e.CreatedAt, e.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken)
        // Out of scope looks the same as missing, so callers can't probe for other employees.
        ?? throw new NotFoundException(nameof(Employee), request.Id);
}
