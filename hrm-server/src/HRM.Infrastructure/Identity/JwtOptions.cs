using System.ComponentModel.DataAnnotations;

namespace HRM.Infrastructure.Identity;

/// <summary>"Jwt" configuration section. <see cref="Key"/> comes from user-secrets (dev) or the JWT_KEY env var (Docker).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>HS256 signing key. At least 32 bytes, so 44+ characters when base64.</summary>
    [Required, MinLength(44)]
    public string Key { get; init; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 30)]
    public int RefreshTokenDays { get; init; } = 7;
}
