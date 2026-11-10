using HRM.Domain.Enums;
using MediatR;

namespace HRM.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>Always for the caller's own employee record.</summary>
public sealed record SubmitLeaveRequestCommand(LeaveType Type, DateOnly StartDate, DateOnly EndDate, string Reason)
    : IRequest<Guid>;
