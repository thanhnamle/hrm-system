using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Common;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Departments.Commands.DeleteDepartment;

public sealed class DeleteDepartmentHandler(IApplicationDbContext db) : IRequestHandler<DeleteDepartmentCommand>
{
    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        // Terminated employees still reference the department for history, so they count too.
        var employees = await db.Employees.CountAsync(e => e.DepartmentId == department.Id, cancellationToken);
        if (employees > 0)
            throw new DomainException($"Department '{department.Code}' still has {employees} employee record(s). Transfer them first.");

        db.Departments.Remove(department);
        await db.SaveChangesAsync(cancellationToken);
    }
}
