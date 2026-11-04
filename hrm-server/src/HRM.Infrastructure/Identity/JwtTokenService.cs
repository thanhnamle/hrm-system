using System.Text;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HRM.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken CreateAccessToken(AuthUser user)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.AccessTokenMinutes);

        var claims = new Dictionary<string, object>
        {
            [HrmClaimTypes.Subject] = user.Id.ToString(),
            [HrmClaimTypes.Email] = user.Email,
            [HrmClaimTypes.Role] = user.Role,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
        };
        if (user.EmployeeId is { } employeeId)
            claims[HrmClaimTypes.EmployeeId] = employeeId.ToString();

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(CreateSigningKey(jwt.Key), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }

    internal static SymmetricSecurityKey CreateSigningKey(string key) => new(Encoding.UTF8.GetBytes(key));
}
