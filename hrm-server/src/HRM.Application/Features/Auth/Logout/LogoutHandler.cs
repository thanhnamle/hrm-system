using HRM.Application.Common.Interfaces;
using MediatR;

namespace HRM.Application.Features.Auth.Logout;

/// <summary>
/// Revokes the refresh token. The access token stays valid until it expires (at most 15 minutes);
/// the SPA drops it from memory.
/// </summary>
public sealed class LogoutHandler(IRefreshTokenService refreshTokenService) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
    }
}
