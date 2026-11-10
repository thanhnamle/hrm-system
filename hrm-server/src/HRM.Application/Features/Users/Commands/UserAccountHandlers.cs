using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Common;
using HRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Users.Commands;

/// <summary>
/// Admin account management. Any change that affects what a user may do (role, lock, password)
/// revokes their refresh tokens, so they must sign in again once their current access token
/// (at most 15 minutes) expires. Admins cannot change their own role or lock themselves out.
/// </summary>
public sealed class UserAccountHandlers(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IIdentityService identityService,
    IRefreshTokenService refreshTokenService) :
    IRequestHandler<CreateUserAccountCommand, Guid>,
    IRequestHandler<ChangeUserRoleCommand>,
    IRequestHandler<LockUserCommand>,
    IRequestHandler<UnlockUserCommand>,
    IRequestHandler<ResetUserPasswordCommand>
{
    public async Task<Guid> Handle(CreateUserAccountCommand request, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.AsNoTracking()
                .Where(e => e.Id == request.EmployeeId)
                .Select(e => new { e.Email, e.Status })
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw ValidationException.For(nameof(request.EmployeeId), "Employee does not exist.");

        if (employee.Status != EmployeeStatus.Active)
            throw new DomainException("A terminated employee cannot get a login account.");

        return await identityService.CreateUserAsync(request.EmployeeId, employee.Email, request.Role, request.Password, cancellationToken);
    }

    public async Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        EnsureNotSelf(request.UserId, "change your own role");
        await identityService.ChangeRoleAsync(request.UserId, request.Role, cancellationToken);
        await refreshTokenService.RevokeAllAsync(request.UserId, "role-changed", cancellationToken);
    }

    public async Task Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        EnsureNotSelf(request.UserId, "lock your own account");
        await identityService.SetLockedAsync(request.UserId, locked: true, cancellationToken);
        await refreshTokenService.RevokeAllAsync(request.UserId, "account-locked", cancellationToken);
    }

    public Task Handle(UnlockUserCommand request, CancellationToken cancellationToken) =>
        identityService.SetLockedAsync(request.UserId, locked: false, cancellationToken);

    public async Task Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        await identityService.ResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
        await refreshTokenService.RevokeAllAsync(request.UserId, "password-reset", cancellationToken);
    }

    private void EnsureNotSelf(Guid userId, string action)
    {
        if (userId == currentUser.UserId)
            throw new DomainException($"You cannot {action}.");
    }
}
