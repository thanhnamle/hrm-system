using Microsoft.AspNetCore.HttpOverrides;

namespace HRM.API.Security;

public static class ReverseProxySetup
{
    /// <summary>
    /// Behind Nginx (ReverseProxy:Enabled), take the client IP and scheme from X-Forwarded-For/-Proto.
    /// Without this every request would come "from Nginx", and the login rate limiter (per client IP)
    /// would lock everyone out together.
    ///
    /// The forwarded headers are trusted from any address because the API container is not published:
    /// only Nginx, on the internal Docker network, can reach it. ForwardLimit = 1 takes only the entry
    /// Nginx added, so a client can't spoof its IP by sending its own X-Forwarded-For.
    /// </summary>
    public static IServiceCollection AddReverseProxySupport(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("ReverseProxy:Enabled"))
            return services;

        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    public static WebApplication UseReverseProxySupport(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("ReverseProxy:Enabled"))
            app.UseForwardedHeaders();
        return app;
    }
}
