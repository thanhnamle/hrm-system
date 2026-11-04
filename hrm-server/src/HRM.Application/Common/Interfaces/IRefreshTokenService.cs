namespace HRM.Application.Common.Interfaces;

/// <summary>
/// Long-lived refresh tokens. Only a SHA-256 hash is stored; the raw value exists only in the client's cookie.
/// </summary>
public interface IRefreshTokenService
{
    Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the presented token and issues its replacement.
    /// Throws <see cref="Exceptions.UnauthorizedException"/> for an unknown, expired or revoked token.
    /// Presenting an already revoked token counts as theft and revokes every token of that user.
    /// </summary>
    Task<RefreshTokenRotation> RotateAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Revokes the token if it exists and is still active. Never throws for unknown tokens.</summary>
    Task RevokeAsync(string token, CancellationToken cancellationToken = default);

    Task RevokeAllAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}

public sealed record IssuedRefreshToken(string Token, DateTimeOffset ExpiresAt);

public sealed record RefreshTokenRotation(Guid UserId, IssuedRefreshToken NewToken);
