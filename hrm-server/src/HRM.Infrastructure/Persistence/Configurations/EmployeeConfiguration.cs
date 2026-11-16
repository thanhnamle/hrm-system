using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees", t =>
            t.HasCheckConstraint("ck_employees_leave_balance", "annual_leave_balance >= 0"));

        builder.Property(e => e.EmployeeCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Position).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.AnnualLeaveBalance).HasPrecision(4, 1);

        builder.HasIndex(e => e.EmployeeCode).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.DepartmentId);

        // string[] maps to a PostgreSQL text[] column.
        builder.Property(e => e.Skills).HasDefaultValueSql("'{}'");

        builder.HasMany(e => e.LeaveRequests)
            .WithOne(r => r.Employee)
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict); // employees are soft-deleted; history must stay

        builder.HasMany(e => e.JobHistory)
            .WithOne()
            .HasForeignKey(h => h.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Removing a qualification from the collection deletes its row.
        builder.HasMany(e => e.Qualifications)
            .WithOne()
            .HasForeignKey(q => q.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class JobHistoryEntryConfiguration : IEntityTypeConfiguration<JobHistoryEntry>
{
    public void Configure(EntityTypeBuilder<JobHistoryEntry> builder)
    {
        builder.ToTable("job_history");

        // Ids are created in the domain (Guid v7). ValueGeneratedNever tells EF that an entry with an
        // id set is still new when it is found in Employee.JobHistory, so it is inserted, not updated.
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.ChangeType).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Position).HasMaxLength(100).IsRequired();
        builder.Property(h => h.Note).HasMaxLength(500);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(h => h.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.EmployeeId, h.EffectiveDate });
    }
}

public class QualificationConfiguration : IEntityTypeConfiguration<Qualification>
{
    public void Configure(EntityTypeBuilder<Qualification> builder)
    {
        builder.ToTable("qualifications", t =>
            t.HasCheckConstraint("ck_qualifications_dates", "end_date IS NULL OR start_date IS NULL OR end_date >= start_date"));

        builder.Property(q => q.Id).ValueGeneratedNever(); // see JobHistoryEntryConfiguration
        builder.Property(q => q.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(q => q.Title).HasMaxLength(Qualification.MaxTitleLength).IsRequired();
        builder.Property(q => q.Organization).HasMaxLength(Qualification.MaxOrganizationLength).IsRequired();
        builder.Property(q => q.Description).HasMaxLength(Qualification.MaxDescriptionLength);

        builder.HasIndex(q => q.EmployeeId);
    }
}

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("employee_documents");

        builder.Property(d => d.FileName).HasMaxLength(200).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Content).IsRequired(); // bytea

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.EmployeeId).IsUnique(); // one CV per employee
    }
}
