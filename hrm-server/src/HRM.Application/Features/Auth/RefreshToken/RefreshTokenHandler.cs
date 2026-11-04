using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using MediatR;

namespace HRM.Application.Features.Auth.RefreshToken;

public sealed class RefreshTokenHandler(
    IIdentityService identityService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService)
    : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    public const string SessionExpired = "Your session has expired. Please sign in again.";

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedException(SessionExpired);

        var rotation = await refreshTokenService.RotateAsync(request.RefreshToken, cancellationToken);

        // The account may have been locked or the employee terminated since the last sign-in.
        var user = await identityService.FindActiveUserAsync(rotation.UserId, cancellationToken);
        if (user is null)
        {
            await refreshTokenService.RevokeAllAsync(rotation.UserId, "account-inactive", cancellationToken);
            throw new UnauthorizedException(SessionExpired);
        }

        var accessToken = jwtTokenService.CreateAccessToken(user);
        return new AuthResult(accessToken, rotation.NewToken, AuthUserDto.From(user));
    }
}
