using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeHandler(IApplicationDbContext db) : IRequestHandler<CreateEmployeeCommand, Guid>
{
    private const string CodePrefix = "EMP-";

    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken))
            throw ValidationException.For(nameof(request.DepartmentId), "Department does not exist.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Employees.AnyAsync(e => e.Email == email, cancellationToken))
            throw new ConflictException($"An employee with email '{email}' already exists.");

        var employee = Employee.Hire(
            await NextEmployeeCodeAsync(cancellationToken),
            request.FirstName, request.LastName, email, request.Position,
            request.DepartmentId, request.HireDate, request.Phone, request.DateOfBirth);

        db.Employees.Add(employee);
        // Two simultaneous creates could pick the same code; the unique index turns that into a 409.
        await db.SaveChangesAsync(cancellationToken);
        return employee.Id;
    }

    private async Task<string> NextEmployeeCodeAsync(CancellationToken cancellationToken)
    {
        // Codes are zero-padded, so the highest string is the highest number.
        var last = await db.Employees
            .Where(e => e.EmployeeCode.StartsWith(CodePrefix))
            .MaxAsync(e => (string?)e.EmployeeCode, cancellationToken);

        var next = last is null ? 1 : int.Parse(last[CodePrefix.Length..]) + 1;
        return $"{CodePrefix}{next:0000}";
    }
}
