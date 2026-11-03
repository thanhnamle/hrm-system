using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Infrastructure.Persistence.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("leave_requests", t =>
            t.HasCheckConstraint("ck_leave_requests_dates", "end_date >= start_date"));

        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Days).HasPrecision(4, 1);
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ReviewNote).HasMaxLength(500);

        // reviewed_by_id → employees.id (no navigation property on the entity)
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(r => r.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);

        // "my requests" and "pending approvals" queries
        builder.HasIndex(r => new { r.EmployeeId, r.Status });
        builder.HasIndex(r => r.Status);
    }
}
