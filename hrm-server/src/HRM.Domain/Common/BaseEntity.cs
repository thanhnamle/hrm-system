namespace HRM.Domain.Common;

public abstract class BaseEntity
{
    // Version 7 GUIDs are time-ordered, so new rows append to the end of the primary-key index.
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}
