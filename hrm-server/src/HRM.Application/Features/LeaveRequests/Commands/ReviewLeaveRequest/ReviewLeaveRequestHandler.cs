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
/// A concurrent second review of the same request fails with 409 (row version on leave_requests).
/// </summary>
public sealed class ReviewLeaveRequestHandler(IApplicationDbContext db, ICurrentUser currentUser, IClock clock)
    : IRequestHandler<ApproveLeaveRequestCommand>, IRequestHandler<RejectLeaveRequestCommand>
{
    public async Task Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var (reviewerId, leaveRequest) = await LoadAsync(request.Id, cancellationToken);
        leaveRequest.Approve(reviewerId, clock.UtcNow, request.Note); // deducts annual leave
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var (reviewerId, leaveRequest) = await LoadAsync(request.Id, cancellationToken);
        leaveRequest.Reject(reviewerId, clock.UtcNow, request.Note);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<(Guid ReviewerId, LeaveRequest Request)> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var reviewerId = currentUser.RequireEmployeeId();

        var leaveRequest = await db.LeaveRequests
                .Include(r => r.Employee) // Approve updates the employee's balance
                .ApplyReviewScope(currentUser)
                .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveRequest), id);

        return (reviewerId, leaveRequest);
    }
}
