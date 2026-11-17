using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.LeaveRequests.Commands.ReviewLeaveRequest;

/// <summary>
/// Approve and reject share the same checks: the reviewer needs an employee record (it is stored
/// as ReviewedById), the request must be in their review scope, and the domain blocks self-review.
///
/// Concurrency: approving deducts from the employee's balance, so two approvals of *different*
/// requests for the same employee would each read the same balance and one deduction would be
/// lost. Both run under the employee lock, one after the other. A second review of the *same*
/// request still fails with 409 (row version on leave_requests).
/// </summary>
public sealed class ReviewLeaveRequestHandler(IApplicationDbContext db, ICurrentUser currentUser, IClock clock)
    : IRequestHandler<ApproveLeaveRequestCommand>, IRequestHandler<RejectLeaveRequestCommand>
{
    public Task Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken) =>
        ReviewAsync(request.Id, (reviewerId, leaveRequest) =>
            leaveRequest.Approve(reviewerId, clock.UtcNow, request.Note), cancellationToken); // deducts annual leave

    public Task Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken) =>
        ReviewAsync(request.Id, (reviewerId, leaveRequest) =>
            leaveRequest.Reject(reviewerId, clock.UtcNow, request.Note), cancellationToken);

    private async Task ReviewAsync(Guid id, Action<Guid, LeaveRequest> review, CancellationToken cancellationToken)
    {
        var reviewerId = currentUser.RequireEmployeeId();

        // Only the owner's id before locking, untracked: the entities are loaded fresh under the lock.
        var employeeId = await db.LeaveRequests.AsNoTracking()
                .ApplyReviewScope(currentUser)
                .Where(r => r.Id == id)
                .Select(r => (Guid?)r.EmployeeId)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveRequest), id);

        await db.WithEmployeeLockAsync(employeeId, async () =>
        {
            var leaveRequest = await db.LeaveRequests
                    .Include(r => r.Employee) // Approve updates the employee's balance
                    .ApplyReviewScope(currentUser)
                    .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(LeaveRequest), id);

            review(reviewerId, leaveRequest);
            return await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }
}
