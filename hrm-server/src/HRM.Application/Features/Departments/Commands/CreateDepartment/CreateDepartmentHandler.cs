using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentHandler(IApplicationDbContext db) : IRequestHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = Department.Create(request.Code, request.Name, request.Description);

        if (await db.Departments.AnyAsync(d => d.Code == department.Code, cancellationToken))
            throw new ConflictException($"Department code '{department.Code}' is already used.");
        if (await db.Departments.AnyAsync(d => d.Name == department.Name, cancellationToken))
            throw new ConflictException($"Department name '{department.Name}' is already used.");

        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);
        return department.Id;
    }
}
