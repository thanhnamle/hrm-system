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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
