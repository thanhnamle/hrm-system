using HRM.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace HRM.API.Security;

public static class AuthorizationSetup
{
    /// <summary>
    /// Layer 1 of docs/rbac.md: one policy per permission, satisfied by the roles that hold it.
    /// Every endpoint requires a signed-in user unless it opts out with [AllowAnonymous].
    /// </summary>
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services) =>
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
                options.AddPolicy(permission, policy => policy.RequireRole(RolePermissions.RolesWith(permission)));

            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });
}
