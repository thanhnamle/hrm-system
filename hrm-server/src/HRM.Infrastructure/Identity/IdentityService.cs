using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Features.Users.Queries.GetUsers;
using HRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider,
    ILogger<IdentityService> logger) : IIdentityService
{
    public async Task<AuthUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            logger.LogWarning("Login failed: unknown email");
            return null;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Login failed for user {UserId}: account locked out", user.Id);
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user); // locks the account after MaxFailedAccessAttempts
            logger.LogWarning("Login failed for user {UserId}: wrong password", user.Id);
            return null;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var authUser = await ToAuthUserAsync(user, cancellationToken);
        if (authUser is null)
            logger.LogWarning("Login refused for user {UserId}: employee terminated or no role", user.Id);
        return authUser;
    }

    public async Task<AuthUser?> FindActiveUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
            return null;

        return await ToAuthUserAsync(user, cancellationToken);
    }

    public Task<Guid?> FindUserIdByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        db.Users.Where(u => u.EmployeeId == employeeId).Select(u => (Guid?)u.Id).SingleOrDefaultAsync(cancellationToken);

    public async Task UpdateLoginEmailAsync(Guid employeeId, string email, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.EmployeeId == employeeId, cancellationToken);
        if (user is null)
            return; // employee has no login yet

        var normalized = userManager.NormalizeEmail(email);
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized && u.Id != user.Id, cancellationToken))
            throw new ConflictException($"Another account already uses '{email}'.");

        // Set directly instead of UserManager.SetEmailAsync, which would save on its own.
        // The user name is the email, so both change; a new security stamp invalidates old Identity tokens.
        user.Email = email;
        user.NormalizedEmail = normalized;
        user.UserName = email;
        user.NormalizedUserName = userManager.NormalizeName(email);
        user.SecurityStamp = Guid.NewGuid().ToString();
    }

    public async Task<IReadOnlyList<UserAccountDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        return await (
                from user in db.Users.AsNoTracking()
                join userRole in db.UserRoles on user.Id equals userRole.UserId
                join role in db.Roles on userRole.RoleId equals role.Id
                orderby user.Email
                select new UserAccountDto(
                    user.Id,
                    user.Email!,
                    role.Name!,
                    user.EmployeeId,
                    user.Employee != null ? user.Employee.FirstName + " " + user.Employee.LastName : null,
                    user.LockoutEnd != null && user.LockoutEnd > now))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateUserAsync(Guid employeeId, string email, string role, string password, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(u => u.EmployeeId == employeeId, cancellationToken))
            throw new ConflictException("This employee already has a login account.");
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException($"Another account already uses '{email}'.");

        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, EmployeeId = employeeId };

        // CreateAsync and AddToRoleAsync each save; a transaction keeps "account without a role" from ever existing.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        ThrowIfFailed(await userManager.CreateAsync(user, password), "Password");
        ThrowIfFailed(await userManager.AddToRoleAsync(user, role), "Role");
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Created account {UserId} for employee {EmployeeId} with role {Role}", user.Id, employeeId, role);
        return user.Id;
    }

    public async Task ChangeRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(userId);
        var current = await userManager.GetRolesAsync(user);
        if (current.SequenceEqual([role]))
            return;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        ThrowIfFailed(await userManager.RemoveFromRolesAsync(user, current), "Role");
        ThrowIfFailed(await userManager.AddToRoleAsync(user, role), "Role");
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Changed role of user {UserId} from {OldRole} to {NewRole}", userId, string.Join(",", current), role);
    }

    public async Task SetLockedAsync(Guid userId, bool locked, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(userId);

        // Seeded and created accounts have LockoutEnabled = true; an admin lock lasts until unlocked.
        ThrowIfFailed(await userManager.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null), "User");
        if (!locked)
            ThrowIfFailed(await userManager.ResetAccessFailedCountAsync(user), "User");

        logger.LogInformation("User {UserId} {Action}", userId, locked ? "locked" : "unlocked");
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(userId);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        ThrowIfFailed(await userManager.ResetPasswordAsync(user, token, newPassword), "NewPassword");
        logger.LogInformation("Password of user {UserId} was reset by an administrator", userId);
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(userId);
        if (!await userManager.CheckPasswordAsync(user, currentPassword))
            throw ValidationException.For("CurrentPassword", "Current password is incorrect.");

        ThrowIfFailed(await userManager.ChangePasswordAsync(user, currentPassword, newPassword), "NewPassword");
        logger.LogInformation("User {UserId} changed their password", userId);
    }

    private async Task<AppUser> FindAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException("User", userId);

    /// <summary>Identity reports policy failures (password too weak, ...) as results; surface them as a 400.</summary>
    private static void ThrowIfFailed(IdentityResult result, string field)
    {
        if (!result.Succeeded)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [field] = result.Errors.Select(e => e.Description).ToArray(),
            });
    }

    /// <summary>Null when the account has no role or belongs to a terminated employee.</summary>
    private async Task<AuthUser?> ToAuthUserAsync(AppUser user, CancellationToken cancellationToken)
    {
        var role = (await userManager.GetRolesAsync(user)).SingleOrDefault();
        if (role is null)
            return null;

        string? fullName = null;
        if (user.EmployeeId is { } employeeId)
        {
            var employee = await db.Employees.AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => new { e.FirstName, e.LastName, e.Status })
                .SingleOrDefaultAsync(cancellationToken);

            if (employee is null || employee.Status != Domain.Enums.EmployeeStatus.Active)
                return null;

            fullName = $"{employee.FirstName} {employee.LastName}";
        }

        return new AuthUser(user.Id, user.Email!, role, user.EmployeeId, fullName);
    }
}
