using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Domain.Entities;
using NSubstitute;

namespace HRM.Application.Tests.Common;

/// <summary>
/// Runs the scope rules in memory (LINQ to objects). The Manager rule needs the Department navigation,
/// which only EF loads, so it is covered by the end-to-end checks instead.
/// </summary>
public class DataScopeTests
{
    private static readonly Guid Engineering = Guid.CreateVersion7();
    private static readonly Employee Huy = Hire("EMP-0001", "huy@hrm.local");
    private static readonly Employee Thao = Hire("EMP-0002", "thao@hrm.local");
    private static readonly IQueryable<Employee> All = new[] { Huy, Thao }.AsQueryable();

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.HR)]
    public void Admin_and_HR_see_everyone(string role) =>
        All.ApplyScope(User(role, employeeId: null)).Count().ShouldBe(2);

    [Fact]
    public void Employee_sees_only_themselves() =>
        All.ApplyScope(User(Roles.Employee, Huy.Id)).ShouldBe([Huy]);

    [Theory]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.Employee)]
    public void Scoped_role_without_employee_record_sees_nothing(string role) =>
        All.ApplyScope(User(role, employeeId: null)).ShouldBeEmpty();

    [Fact]
    public void Unknown_role_sees_nothing() =>
        All.ApplyScope(User("Auditor", Huy.Id)).ShouldBeEmpty();

    [Theory]
    [InlineData(Roles.Employee)]
    [InlineData("Auditor")]
    public void Only_reviewers_have_a_review_scope(string role)
    {
        var requester = Hire("EMP-0003", "khoa@hrm.local"); // fresh: the shared employees must not collect requests
        var requests = new[] { requester.RequestLeave(Domain.Enums.LeaveType.Sick, Monday, Monday, "Flu", Monday) }.AsQueryable();

        requests.ApplyReviewScope(User(role, Thao.Id)).ShouldBeEmpty();
        requests.ApplyReviewScope(User(Roles.HR, Thao.Id)).Count().ShouldBe(1);
    }

    [Fact]
    public void RequireEmployeeId_rejects_accounts_without_employee() =>
        Should.Throw<ForbiddenAccessException>(() => User(Roles.Admin, employeeId: null).RequireEmployeeId());

    private static readonly DateOnly Monday = new(2030, 1, 7);

    private static Employee Hire(string code, string email) =>
        Employee.Hire(code, "First", "Last", email, "Developer", Engineering, new DateOnly(2024, 1, 1));

    private static ICurrentUser User(string role, Guid? employeeId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.Role.Returns(role);
        user.EmployeeId.Returns(employeeId);
        return user;
    }
}
