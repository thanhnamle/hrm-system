using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeHandler(IApplicationDbContext db, IEmployeeCodeGenerator codeGenerator)
    : IRequestHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken))
            throw ValidationException.For(nameof(request.DepartmentId), "Department does not exist.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Employees.AnyAsync(e => e.Email == email, cancellationToken))
            throw new ConflictException($"An employee with email '{email}' already exists.");

        var employee = Employee.Hire(
            await codeGenerator.NextAsync(cancellationToken),
            request.FirstName, request.LastName, email, request.Position,
            request.DepartmentId, request.HireDate, request.Phone, request.DateOfBirth);

        db.Employees.Add(employee);
        await db.SaveChangesAsync(cancellationToken);
        return employee.Id;
    }
}
