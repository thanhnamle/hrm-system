using HRM.Application.Features.Users.Queries.GetUsers;

namespace HRM.Application.Common.Interfaces;

/// <summary>
/// Login accounts (ASP.NET Core Identity), implemented in Infrastructure.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Checks the password and counts failures towards lockout.
    /// Returns null for a wrong email or password, a locked account or a terminated employee. Callers
    /// must not tell these apart, so the response never reveals whether an account exists.
    /// </summary>
    Task<AuthUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Returns null when the account no longer exists or may no longer sign in.</summary>
    Task<AuthUser?> FindActiveUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAccountDto>> GetUsersAsync(CancellationToken cancellationToken = default);
}

/// <summary>An account that is allowed to sign in. One role per user.</summary>
public sealed record AuthUser(Guid Id, string Email, string Role, Guid? EmployeeId, string? FullName);
