using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Infrastructure.Identity;

public sealed class RefreshTokenService(
    AppDbContext db,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    private const string InvalidToken = "Your session has expired. Please sign in again.";

    public async Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (issued, _) = Add(userId, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return issued;
    }

    public async Task<RefreshTokenRotation> RotateAsync(string token, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == Hash(token), cancellationToken)
            ?? throw new UnauthorizedException(InvalidToken);

        if (stored.IsRevoked)
        {
            // A revoked token is only presented again if someone kept a copy: assume it was stolen.
            logger.LogWarning("Refresh token reuse detected for user {UserId}; revoking all sessions", stored.UserId);
            await RevokeAllAsync(stored.UserId, "reuse-detected", cancellationToken);
            throw new UnauthorizedException(InvalidToken);
        }

        if (stored.IsExpired(now))
            throw new UnauthorizedException(InvalidToken);

        var (issued, replacement) = Add(stored.UserId, now);
        stored.Revoke(now, "rotated", replacement.TokenHash);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request rotated this token first.
            throw new UnauthorizedException(InvalidToken);
        }

        return new RefreshTokenRotation(stored.UserId, issued);
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == Hash(token), cancellationToken);
        if (stored is null || !stored.IsActive(now))
            return;

        stored.Revoke(now, "logout");
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task RevokeAllAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, reason), cancellationToken);
    }

    private (IssuedRefreshToken Issued, RefreshToken Entity) Add(Guid userId, DateTimeOffset now)
    {
        var raw = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
        var entity = RefreshToken.Create(userId, Hash(raw), now, TimeSpan.FromDays(options.Value.RefreshTokenDays));
        db.RefreshTokens.Add(entity);
        return (new IssuedRefreshToken(raw, entity.ExpiresAt), entity);
    }

    private static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
