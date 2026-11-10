using FluentValidation;
using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using MediatR;

namespace HRM.Application.Features.Auth.ChangePassword;

/// <summary>
/// Any signed-in user changes their own password. Every session is revoked afterwards, so a thief
/// holding an old refresh token is cut off; the user signs in again with the new password.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(128)
            .NotEqual(x => x.CurrentPassword).WithMessage("The new password must be different from the current one.");
    }
}

public sealed class ChangePasswordHandler(
    ICurrentUser currentUser,
    IIdentityService identityService,
    IRefreshTokenService refreshTokenService) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Not signed in.");

        await identityService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        await refreshTokenService.RevokeAllAsync(userId, "password-changed", cancellationToken);
    }
}
