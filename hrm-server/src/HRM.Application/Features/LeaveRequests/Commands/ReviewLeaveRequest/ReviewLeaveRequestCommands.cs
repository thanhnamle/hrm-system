using MediatR;

namespace HRM.Application.Features.LeaveRequests.Commands.ReviewLeaveRequest;

public sealed record ApproveLeaveRequestCommand(Guid Id, string? Note) : IRequest;

/// <summary>A rejection always needs a note, so the employee knows why.</summary>
public sealed record RejectLeaveRequestCommand(Guid Id, string Note) : IRequest;
