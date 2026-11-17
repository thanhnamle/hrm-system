namespace HRM.Application.Common.Interfaces;

/// <summary>
/// Hands out employee codes (EMP-0001, EMP-0002, …). Every call returns a code nobody else got,
/// even when several employees are created at the same moment. A code taken by a create that
/// then fails is not reused, so there can be gaps.
/// </summary>
public interface IEmployeeCodeGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
