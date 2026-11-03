namespace HRM.Domain.Enums;

public enum LeaveType
{
    /// <summary>Paid leave; consumes the employee's annual leave balance.</summary>
    Annual,

    /// <summary>Does not consume the annual balance.</summary>
    Sick,

    /// <summary>Does not consume the annual balance.</summary>
    Unpaid,
}
