using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using MediatR;

namespace HRM.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IIdentityService identityService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService)
    : IRequestHandler<LoginCommand, AuthResult>
{
    public const string InvalidCredentials = "Invalid email or password.";

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.AuthenticateAsync(request.Email, request.Password, cancellationToken)
            ?? throw new UnauthorizedException(InvalidCredentials);

        var accessToken = jwtTokenService.CreateAccessToken(user);
        var refreshToken = await refreshTokenService.IssueAsync(user.Id, cancellationToken);

        return new AuthResult(accessToken, refreshToken, AuthUserDto.From(user));
    }
}
