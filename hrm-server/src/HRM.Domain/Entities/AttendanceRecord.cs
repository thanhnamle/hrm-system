using HRM.Domain.Common;

namespace HRM.Domain.Entities;

/// <summary>
/// One row per employee per work day. "Only one check-in per day" is enforced by a unique index
/// on (employee_id, work_date) plus a check in the CheckIn handler.
/// </summary>
public class AttendanceRecord : AuditableEntity
{
    private AttendanceRecord() { } // EF Core

    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = null!;

    /// <summary>The local (company time zone) date the record belongs to.</summary>
    public DateOnly WorkDate { get; private set; }
    public DateTimeOffset CheckInAt { get; private set; }
    public DateTimeOffset? CheckOutAt { get; private set; }

    public bool IsCheckedOut => CheckOutAt.HasValue;

    public static AttendanceRecord CheckIn(Employee employee, DateOnly workDate, DateTimeOffset checkInAt)
    {
        if (!employee.IsActive)
            throw new DomainException("A terminated employee cannot check in.");

        return new AttendanceRecord
        {
            Employee = employee,
            EmployeeId = employee.Id,
            WorkDate = workDate,
            CheckInAt = checkInAt,
        };
    }

    public void CheckOut(DateTimeOffset checkOutAt)
    {
        if (IsCheckedOut)
            throw new DomainException("Already checked out for this day.");
        if (checkOutAt <= CheckInAt)
            throw new DomainException("Check-out must be after check-in.");
        CheckOutAt = checkOutAt;
    }
}
