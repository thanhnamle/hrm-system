namespace HRM.Domain.Common;

/// <summary>
/// Entity whose audit columns are filled automatically by the EF Core interceptor on SaveChanges.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
