using System.Security.Cryptography;
using System.Text;
using HRM.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HRM.Infrastructure.Identity;

/// <summary>
/// Makes access tokens stop working as soon as an account changes in a way that should end its
/// sessions: the employee is terminated, the account is locked, its role or password changes.
///
/// Each access token carries a fingerprint of the account's Identity security stamp ("sst" claim).
/// Those changes give the account a new stamp, so every request compares the token's fingerprint
/// with the current one. The current one is cached for <see cref="CacheFor"/> per user (one small
/// lookup per user per 30 s, not per request) and evicted on the spot when this API changes the
/// account; with several API instances, another instance notices within the cache time.
/// </summary>
public sealed class SessionValidator(IIdentityService identityService, IMemoryCache cache)
{
    public static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

    /// <summary>
    /// A short, one-way fingerprint of the security stamp. The token is readable by the client, so
    /// it carries this instead of the stamp itself.
    /// </summary>
    public static string Fingerprint(string? securityStamp) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp ?? string.Empty)))[..22];

    /// <summary>True while the account may still use a token issued with this fingerprint.</summary>
    public async Task<bool> IsCurrentAsync(Guid userId, string? fingerprint, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fingerprint))
            return false; // issued before session checks existed: the client refreshes and gets a new one

        var current = await cache.GetOrCreateAsync(Key(userId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            // Null when the account is gone, locked, has no role, or its employee has left.
            var user = await identityService.FindActiveUserAsync(userId, cancellationToken);
            return user?.SessionStamp ?? string.Empty;
        });

        return !string.IsNullOrEmpty(current) && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(current), Encoding.UTF8.GetBytes(fingerprint));
    }

    /// <summary>Forget the cached state so the next request sees the change immediately.</summary>
    public static void Evict(IMemoryCache cache, Guid userId) => cache.Remove(Key(userId));

    private static string Key(Guid userId) => $"session:{userId}";
}
