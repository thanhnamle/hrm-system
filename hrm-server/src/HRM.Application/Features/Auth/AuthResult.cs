using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;

namespace HRM.Application.Features.Auth;

/// <summary>
/// Result of login and refresh. The API puts <see cref="RefreshToken"/> in an HttpOnly cookie
/// and returns the rest in the body.
/// </summary>
public sealed record AuthResult(AccessToken AccessToken, IssuedRefreshToken RefreshToken, AuthUserDto User);

/// <summary>The signed-in user as the SPA sees it. <see cref="Permissions"/> drives what the UI shows.</summary>
public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string Role,
    Guid? EmployeeId,
    string? FullName,
    IReadOnlyList<string> Permissions)
{
    public static AuthUserDto From(AuthUser user) =>
        new(user.Id, user.Email, user.Role, user.EmployeeId, user.FullName, RolePermissions.For(user.Role));
}
