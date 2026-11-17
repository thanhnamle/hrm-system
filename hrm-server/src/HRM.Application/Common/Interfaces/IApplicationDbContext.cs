using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Common.Interfaces;

/// <summary>
/// The persistence contract handlers depend on. Implemented by AppDbContext in Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<Department> Departments { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<JobHistoryEntry> JobHistory { get; }
    DbSet<Qualification> Qualifications { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="work"/> in a transaction that first locks the employee's row, so changes
    /// to one employee's leave (submit, approve, terminate) happen one after another instead of
    /// interleaving. Two requests can then never both pass a check made on the same data, and a
    /// balance can't be overwritten by a concurrent approval. Load the entities you change inside
    /// <paramref name="work"/>: anything already tracked before the lock may be stale.
    /// Locks on different employees don't block each other.
    /// </summary>
    Task<T> WithEmployeeLockAsync<T>(Guid employeeId, Func<Task<T>> work, CancellationToken cancellationToken = default);
}
