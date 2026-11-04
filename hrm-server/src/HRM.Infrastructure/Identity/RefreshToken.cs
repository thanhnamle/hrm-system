namespace HRM.Infrastructure.Identity;

/// <summary>
/// One issued refresh token. Rotation links each token to its replacement, so a reused token can be traced.
/// </summary>
public class RefreshToken
{
    private RefreshToken() { } // EF Core

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid UserId { get; private set; }

    /// <summary>SHA-256 of the raw token, lowercase hex. The raw value is never stored.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>Concurrency token (PostgreSQL xmin): two parallel refreshes cannot both rotate the same token.</summary>
    public uint Version { get; private set; }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + lifetime,
    };

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    public void Revoke(DateTimeOffset now, string reason, string? replacedByTokenHash = null)
    {
        RevokedAt = now;
        RevokedReason = reason;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
