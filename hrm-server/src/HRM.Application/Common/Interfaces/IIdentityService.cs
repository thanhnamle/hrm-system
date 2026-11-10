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

    Task<Guid?> FindUserIdByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Points the employee's login (if any) at a new email. The change is only tracked: it is saved
    /// together with the employee by the caller's SaveChangesAsync, so both succeed or neither does.
    /// Throws <see cref="Exceptions.ConflictException"/> if another account already uses the email.
    /// </summary>
    Task UpdateLoginEmailAsync(Guid employeeId, string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAccountDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    // --- Account management. All throw NotFoundException for an unknown user id, and
    //     ValidationException (field "Password"/"NewPassword") when a password breaks the policy. ---

    /// <summary>Creates the login for an employee; the login email is the employee's email.</summary>
    Task<Guid> CreateUserAsync(Guid employeeId, string email, string role, string password, CancellationToken cancellationToken = default);

    Task ChangeRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    /// <summary>Locked accounts cannot sign in or refresh until unlocked.</summary>
    Task SetLockedAsync(Guid userId, bool locked, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>Throws ValidationException (field "CurrentPassword") if the current password is wrong.</summary>
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

/// <summary>An account that is allowed to sign in. One role per user.</summary>
public sealed record AuthUser(Guid Id, string Email, string Role, Guid? EmployeeId, string? FullName);
