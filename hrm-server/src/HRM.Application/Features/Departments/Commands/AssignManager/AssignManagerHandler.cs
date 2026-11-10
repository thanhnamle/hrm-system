using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Departments.Commands.AssignManager;

public sealed class AssignManagerHandler(IApplicationDbContext db)
    : IRequestHandler<AssignManagerCommand>, IRequestHandler<RemoveManagerCommand>
{
    public async Task Handle(AssignManagerCommand request, CancellationToken cancellationToken)
    {
        var department = await FindAsync(request.DepartmentId, cancellationToken);
        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw ValidationException.For(nameof(request.EmployeeId), "Employee does not exist.");

        department.AssignManager(employee); // must be an active member of the department
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(RemoveManagerCommand request, CancellationToken cancellationToken)
    {
        var department = await FindAsync(request.DepartmentId, cancellationToken);
        department.RemoveManager();
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Department> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Departments.SingleOrDefaultAsync(d => d.Id == id, cancellationToken)
        ?? throw new NotFoundException(nameof(Department), id);
}
