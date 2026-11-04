using HRM.API.Security;
using HRM.Application.Features.Auth;
using HRM.Application.Features.Auth.GetMe;
using HRM.Application.Features.Auth.Login;
using HRM.Application.Features.Auth.Logout;
using HRM.Application.Features.Auth.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HRM.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    private const string RefreshCookieName = "hrm_refresh";
    private const string RefreshCookiePath = "/api/auth"; // the cookie is only sent to these endpoints

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingSetup.LoginPolicy)]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(ToResponse(result));
    }

    /// <summary>Uses the HttpOnly refresh cookie; the SPA calls this on page load to restore the session.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshTokenCommand(Request.Cookies[RefreshCookieName]), cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(Request.Cookies[RefreshCookieName]), cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, CookieOptions(expires: null));
        return NoContent();
    }

    [HttpGet("me")]
    public Task<AuthUserDto> Me(CancellationToken cancellationToken) =>
        sender.Send(new GetMeQuery(), cancellationToken);

    private AuthResponse ToResponse(AuthResult result)
    {
        Response.Cookies.Append(RefreshCookieName, result.RefreshToken.Token, CookieOptions(result.RefreshToken.ExpiresAt));
        return new AuthResponse(result.AccessToken.Token, result.AccessToken.ExpiresAt, result.User);
    }

    private static CookieOptions CookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true, // browsers still accept it on http://localhost
        SameSite = SameSiteMode.Strict,
        Path = RefreshCookiePath,
        Expires = expires,
        IsEssential = true,
    };
}

/// <summary>The refresh token is not in the body: it is only ever in the HttpOnly cookie.</summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthUserDto User);
