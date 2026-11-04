using System.Security.Claims;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using Microsoft.AspNetCore.Http;

namespace HRM.Infrastructure.Identity;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => ParseGuid(Principal?.FindFirstValue(HrmClaimTypes.Subject));

    public Guid? EmployeeId => ParseGuid(Principal?.FindFirstValue(HrmClaimTypes.EmployeeId));

    public string? Role => Principal?.FindFirstValue(HrmClaimTypes.Role);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
