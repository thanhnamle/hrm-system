using MediatR;

namespace HRM.Application.Features.LeaveRequests.Commands.CancelLeaveRequest;

/// <summary>Only the owner can cancel, and only while the request is pending.</summary>
public sealed record CancelLeaveRequestCommand(Guid Id) : IRequest;
