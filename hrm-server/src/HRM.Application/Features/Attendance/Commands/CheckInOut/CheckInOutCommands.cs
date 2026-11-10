using MediatR;

namespace HRM.Application.Features.Attendance.Commands.CheckInOut;

/// <summary>Records the server time for the caller's own employee record; the client sends no times.</summary>
public sealed record CheckInCommand : IRequest<AttendanceRecordDto>;

public sealed record CheckOutCommand : IRequest<AttendanceRecordDto>;
