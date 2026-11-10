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
