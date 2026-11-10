using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Common;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Employees.Commands.TerminateEmployee;

/// <summary>
/// Soft delete. Pending leave is cancelled, any department they manage loses its manager,
/// and every session of their account is revoked so they are signed out immediately.
/// </summary>
public sealed class TerminateEmployeeHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IIdentityService identityService,
    IRefreshTokenService refreshTokenService)
    : IRequestHandler<TerminateEmployeeCommand>
{
    public async Task Handle(TerminateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == currentUser.EmployeeId)
            throw new DomainException("You cannot terminate your own employee record.");

        var employee = await db.Employees
                .Include(e => e.LeaveRequests.Where(r => r.Status == LeaveStatus.Pending))
                .SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.Id);

        employee.Terminate();

        var managed = await db.Departments.Where(d => d.ManagerId == employee.Id).ToListAsync(cancellationToken);
        managed.ForEach(d => d.RemoveManager());

        await db.SaveChangesAsync(cancellationToken);

        if (await identityService.FindUserIdByEmployeeIdAsync(employee.Id, cancellationToken) is { } userId)
            await refreshTokenService.RevokeAllAsync(userId, "employee-terminated", cancellationToken);
    }
}
