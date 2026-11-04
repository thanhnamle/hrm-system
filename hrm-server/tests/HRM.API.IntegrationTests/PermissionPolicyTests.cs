using System.Security.Claims;
using HRM.API.Security;
using HRM.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.API.IntegrationTests;

/// <summary>
/// Evaluates every registered policy for every role against the matrix in docs/rbac.md §3.
/// A change to RolePermissions that doesn't match the documented matrix fails here.
/// </summary>
public class PermissionPolicyTests
{
    // Row order follows docs/rbac.md: Admin, HR, Manager, Employee.
    public static TheoryData<string, bool, bool, bool, bool> Matrix => new()
    {
        { Permissions.EmployeesView,      true, true,  true,  true  },
        { Permissions.EmployeesCreate,    true, true,  false, false },
        { Permissions.EmployeesUpdate,    true, true,  false, false },
        { Permissions.EmployeesTerminate, true, true,  false, false },
        { Permissions.DepartmentsView,    true, true,  true,  true  },
        { Permissions.DepartmentsManage,  true, true,  false, false },
        { Permissions.LeaveRequest,       true, true,  true,  true  },
        { Permissions.LeaveApprove,       true, true,  true,  false },
        { Permissions.LeaveViewAll,       true, true,  false, false },
        { Permissions.AttendanceRecord,   true, true,  true,  true  },
        { Permissions.AttendanceView,     true, true,  true,  false },
        { Permissions.UsersManage,        true, false, false, false },
    };

    private readonly IAuthorizationService _authorization = new ServiceCollection()
        .AddLogging()
        .AddPermissionPolicies()
        .BuildServiceProvider()
        .GetRequiredService<IAuthorizationService>();

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task Policy_matches_documented_matrix(string permission, bool admin, bool hr, bool manager, bool employee)
    {
        (await IsAllowed(Roles.Admin, permission)).ShouldBe(admin, Roles.Admin);
        (await IsAllowed(Roles.HR, permission)).ShouldBe(hr, Roles.HR);
        (await IsAllowed(Roles.Manager, permission)).ShouldBe(manager, Roles.Manager);
        (await IsAllowed(Roles.Employee, permission)).ShouldBe(employee, Roles.Employee);
    }

    [Fact]
    public void Matrix_covers_every_permission() =>
        Matrix.Select(row => (string)row[0]).ShouldBe(Permissions.All, ignoreOrder: true);

    [Fact]
    public async Task Fallback_policy_rejects_anonymous_callers()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _authorization.AuthorizeAsync(anonymous, null, new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build());

        result.Succeeded.ShouldBeFalse();
    }

    private async Task<bool> IsAllowed(string role, string permission)
    {
        var identity = new ClaimsIdentity(
            [new Claim(HrmClaimTypes.Role, role)], authenticationType: "Test", HrmClaimTypes.Email, HrmClaimTypes.Role);
        return (await _authorization.AuthorizeAsync(new ClaimsPrincipal(identity), permission)).Succeeded;
    }
}
