using System.Security.Claims;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using Microsoft.AspNetCore.Http;

namespace HRM.Infrastructure.Identity;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    // JwtBearer maps "sub" to NameIdentifier by default; check both so either mapping works.
    public Guid? UserId => ParseGuid(Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub"));

    public Guid? EmployeeId => ParseGuid(Principal?.FindFirstValue(HrmClaimTypes.EmployeeId));

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
