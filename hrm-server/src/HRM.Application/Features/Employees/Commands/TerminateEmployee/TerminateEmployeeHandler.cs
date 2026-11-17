using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Common;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Employees.Commands.TerminateEmployee;

/// <summary>
/// Soft delete, recorded in the job history. Pending leave is cancelled, any department they manage loses its manager,
/// and their account is signed out everywhere: refresh tokens are revoked and access tokens already
/// issued stop working on their next request (the account's security stamp changes).
///
/// The termination is saved first; if signing out then fails, the employee is still terminated and
/// still can't get in: login and refresh both refuse an employee who has left, and the session check
/// on every request finds the account no longer active.
/// </summary>
public sealed class TerminateEmployeeHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IIdentityService identityService,
    IRefreshTokenService refreshTokenService,
    IClock clock)
    : IRequestHandler<TerminateEmployeeCommand>
{
    public async Task Handle(TerminateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == currentUser.EmployeeId)
            throw new DomainException("You cannot terminate your own employee record.");

        // Under the employee lock: a leave request submitted at the same moment either lands first
        // (and is then withdrawn here) or waits and is refused because the employee has left.
        var employee = await db.WithEmployeeLockAsync(request.Id, async () =>
        {
            var employee = await db.Employees
                    .Include(e => e.LeaveRequests.Where(r => r.Status == LeaveStatus.Pending))
                    .SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Employee), request.Id);

            employee.Terminate(clock.Today, request.Note);

            var managed = await db.Departments.Where(d => d.ManagerId == employee.Id).ToListAsync(cancellationToken);
            managed.ForEach(d => d.RemoveManager());

            await db.SaveChangesAsync(cancellationToken);
            return employee;
        }, cancellationToken);

        if (await identityService.FindUserIdByEmployeeIdAsync(employee.Id, cancellationToken) is { } userId)
        {
            await refreshTokenService.RevokeAllAsync(userId, "employee-terminated", cancellationToken);
            await identityService.EndSessionsAsync(userId, cancellationToken);
        }
    }
}
