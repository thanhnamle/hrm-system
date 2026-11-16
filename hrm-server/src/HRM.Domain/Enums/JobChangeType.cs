namespace HRM.Domain.Enums;

/// <summary>What happened in a job history entry. Promotions are position changes with a note.</summary>
public enum JobChangeType
{
    Hired,
    PositionChanged,
    Transferred,
    Terminated,
}
