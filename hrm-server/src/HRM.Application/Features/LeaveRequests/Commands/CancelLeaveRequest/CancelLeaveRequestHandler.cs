using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.LeaveRequests.Commands.CancelLeaveRequest;

public sealed class CancelLeaveRequestHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CancelLeaveRequestCommand>
{
    public async Task Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.RequireEmployeeId();

        // Someone else's request is "not found" rather than "forbidden".
        var leaveRequest = await db.LeaveRequests
                .SingleOrDefaultAsync(r => r.Id == request.Id && r.EmployeeId == employeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveRequest), request.Id);

        leaveRequest.Cancel(employeeId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
