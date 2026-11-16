using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeHandler(IApplicationDbContext db, IIdentityService identityService, IClock clock)
    : IRequestHandler<UpdateEmployeeCommand>
{
    public async Task Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.Id);

        employee.UpdateDetails(request.FirstName, request.LastName, request.Phone, request.DateOfBirth);

        var email = request.Email.Trim().ToLowerInvariant();
        if (email != employee.Email)
        {
            if (await db.Employees.AnyAsync(e => e.Email == email && e.Id != employee.Id, cancellationToken))
                throw new ConflictException($"An employee with email '{email}' already exists.");

            employee.ChangeEmail(email);
            await identityService.UpdateLoginEmailAsync(employee.Id, email, cancellationToken);
        }

        if (request.DepartmentId != employee.DepartmentId)
        {
            if (!await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken))
                throw ValidationException.For(nameof(request.DepartmentId), "Department does not exist.");

            // A manager must belong to the department they manage, so a transfer ends that role.
            var managed = await db.Departments.Where(d => d.ManagerId == employee.Id).ToListAsync(cancellationToken);
            managed.ForEach(d => d.RemoveManager());
        }

        // Records a Transferred or PositionChanged entry in the job history (nothing if unchanged).
        employee.ChangeJob(request.Position, request.DepartmentId, clock.Today, request.ChangeNote);

        await db.SaveChangesAsync(cancellationToken); // employee, history and login email in one transaction
    }
}
