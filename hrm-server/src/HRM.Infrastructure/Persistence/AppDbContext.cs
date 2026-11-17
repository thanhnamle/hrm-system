using HRM.Application.Common.Interfaces;
using HRM.Domain.Entities;
using HRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRM.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IApplicationDbContext
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<JobHistoryEntry> JobHistory => Set<JobHistoryEntry>();
    public DbSet<Qualification> Qualifications => Set<Qualification>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();

    // Auth only; not exposed to the Application layer through IApplicationDbContext.
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public async Task<T> WithEmployeeLockAsync<T>(Guid employeeId, Func<Task<T>> work, CancellationToken cancellationToken = default)
    {
        // Already inside a transaction (a caller holds the lock or wants a wider unit): just lock and run.
        if (Database.CurrentTransaction is not null)
        {
            await LockEmployeeAsync(employeeId, cancellationToken);
            return await work();
        }

        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        await LockEmployeeAsync(employeeId, cancellationToken);
        var result = await work();
        await transaction.CommitAsync(cancellationToken); // releases the lock
        return result;
    }

    /// <summary>
    /// PostgreSQL row lock, held until the transaction ends: a second transaction locking the same
    /// employee waits here, then reads what the first one committed.
    /// </summary>
    private Task LockEmployeeAsync(Guid employeeId, CancellationToken cancellationToken) =>
        Database.ExecuteSqlAsync($"SELECT 1 FROM employees WHERE id = {employeeId} FOR UPDATE", cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's model first, so our configurations (table renames etc.) can override it.
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Employee codes; see EmployeeCodeGenerator.
        modelBuilder.HasSequence<long>(EmployeeCodeGenerator.SequenceName);
    }
}
