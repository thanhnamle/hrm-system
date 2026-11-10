using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Departments.Commands.UpdateDepartment;

public sealed class UpdateDepartmentHandler(IApplicationDbContext db) : IRequestHandler<UpdateDepartmentCommand>
{
    public async Task Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        department.Update(request.Name, request.Description);

        if (await db.Departments.AnyAsync(d => d.Name == department.Name && d.Id != department.Id, cancellationToken))
            throw new ConflictException($"Department name '{department.Name}' is already used.");

        await db.SaveChangesAsync(cancellationToken);
    }
}
