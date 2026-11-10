using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace HRM.API.Security;

public static class RateLimitingSetup
{
    public const string LoginPolicy = "login";

    /// <summary>
    /// 5 login attempts per minute per client IP (RateLimiting:LoginPermitLimit; integration tests raise it). Works together with Identity lockout (5 failures per account).
    /// Behind Nginx the client IP comes from X-Forwarded-For, which is wired up with the Docker setup on Day 10.
    /// </summary>
    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:LoginPermitLimit", 5);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                return ValueTask.CompletedTask;
            };

            options.AddPolicy(LoginPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });
    }
}
