namespace HRM.Application.Common.Interfaces;

/// <summary>
/// The persistence contract handlers depend on. Implemented by AppDbContext in Infrastructure.
/// DbSet properties are added together with the entities (Day 2).
/// </summary>
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
