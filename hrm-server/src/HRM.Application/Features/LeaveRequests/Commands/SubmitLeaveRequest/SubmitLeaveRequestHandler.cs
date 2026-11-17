using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>
/// The overlap and balance checks in <see cref="Employee.RequestLeave"/> read the employee's other
/// requests, so two submits at the same moment (a double click) could both pass them. The employee
/// lock makes the second one wait and then see the first: it is refused as an overlap.
/// </summary>
public sealed class SubmitLeaveRequestHandler(IApplicationDbContext db, ICurrentUser currentUser, IClock clock)
    : IRequestHandler<SubmitLeaveRequestCommand, Guid>
{
    public Task<Guid> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.RequireEmployeeId();

        return db.WithEmployeeLockAsync(employeeId, async () =>
        {
            // RequestLeave checks overlap and balance against the active (pending + approved) requests.
            var employee = await db.Employees
                    .Include(e => e.LeaveRequests.Where(r => r.Status == LeaveStatus.Pending || r.Status == LeaveStatus.Approved))
                    .SingleOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
                ?? throw new NotFoundException(nameof(Employee), employeeId);

            var leaveRequest = employee.RequestLeave(request.Type, request.StartDate, request.EndDate, request.Reason, clock.Today);

            // Added explicitly: the key is set client-side, so EF would otherwise treat it as an existing row.
            db.LeaveRequests.Add(leaveRequest);
            await db.SaveChangesAsync(cancellationToken);
            return leaveRequest.Id;
        }, cancellationToken);
    }
}
