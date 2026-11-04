namespace HRM.Application.Common.Interfaces;

/// <summary>Issues short-lived signed access tokens (JWT).</summary>
public interface IJwtTokenService
{
    AccessToken CreateAccessToken(AuthUser user);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
