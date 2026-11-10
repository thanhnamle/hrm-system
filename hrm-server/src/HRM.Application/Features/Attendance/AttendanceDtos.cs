namespace HRM.Application.Features.Attendance;

public sealed record AttendanceRecordDto(Guid Id, DateOnly WorkDate, DateTimeOffset CheckInAt, DateTimeOffset? CheckOutAt)
{
    public double? WorkedHours => CheckOutAt is { } checkOut ? Math.Round((checkOut - CheckInAt).TotalHours, 2) : null;
}

/// <summary>One row per active employee in scope for a given day, whether or not they checked in.</summary>
public sealed record TeamAttendanceDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    DateTimeOffset? CheckInAt,
    DateTimeOffset? CheckOutAt,
    bool OnLeave)
{
    public string Status => OnLeave ? "OnLeave"
        : CheckOutAt is not null ? "CheckedOut"
        : CheckInAt is not null ? "Present"
        : "Absent";
}
