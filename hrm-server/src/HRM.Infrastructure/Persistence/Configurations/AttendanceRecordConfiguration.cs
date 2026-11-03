using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records", t =>
            t.HasCheckConstraint("ck_attendance_records_check_out", "check_out_at IS NULL OR check_out_at > check_in_at"));

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // One check-in per employee per day
        builder.HasIndex(a => new { a.EmployeeId, a.WorkDate }).IsUnique();
    }
}
