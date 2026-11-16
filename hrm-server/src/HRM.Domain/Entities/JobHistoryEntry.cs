using HRM.Domain.Common;
using HRM.Domain.Enums;

namespace HRM.Domain.Entities;

/// <summary>
/// One event in an employee's career at the company, with the position and department from that
/// moment on. Created only by <see cref="Employee"/>, so the history can't drift from the record.
/// CreatedBy (audit) tells who made the change.
/// </summary>
public class JobHistoryEntry : AuditableEntity
{
    private JobHistoryEntry() { } // EF Core

    internal JobHistoryEntry(Employee employee, JobChangeType changeType, DateOnly effectiveDate, string? note)
    {
        EmployeeId = employee.Id;
        ChangeType = changeType;
        EffectiveDate = effectiveDate;
        Position = employee.Position;
        DepartmentId = employee.DepartmentId;
        Note = Guard.Optional(note);
    }

    public Guid EmployeeId { get; private set; }
    public JobChangeType ChangeType { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public string Position { get; private set; } = string.Empty;
    public Guid DepartmentId { get; private set; }
    public string? Note { get; private set; }
}
