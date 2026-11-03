using HRM.Domain.Common;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using static HRM.Domain.Tests.TestData;

namespace HRM.Domain.Tests;

public class EmployeeTests
{
    [Fact]
    public void Hire_creates_active_employee_with_default_balance_and_normalized_fields()
    {
        var employee = NewEmployee();

        employee.Status.ShouldBe(EmployeeStatus.Active);
        employee.AnnualLeaveBalance.ShouldBe(Employee.DefaultAnnualLeaveDays);
        employee.EmployeeCode.ShouldBe("EMP-0001");
        employee.Email.ShouldBe("an.nguyen@hrm.local");
        employee.FullName.ShouldBe("An Nguyen");
    }

    [Theory]
    [InlineData("", "Nguyen", "a@b.c")]
    [InlineData("An", " ", "a@b.c")]
    [InlineData("An", "Nguyen", "not-an-email")]
    public void Hire_rejects_missing_or_invalid_fields(string firstName, string lastName, string email)
    {
        Should.Throw<DomainException>(() =>
            Employee.Hire("EMP-1", firstName, lastName, email, "Dev", Guid.CreateVersion7(), Monday));
    }

    [Fact]
    public void Hire_rejects_birth_date_after_hire_date()
    {
        Should.Throw<DomainException>(() =>
            Employee.Hire("EMP-1", "An", "Nguyen", "a@b.c", "Dev", Guid.CreateVersion7(), Monday, dateOfBirth: Monday.AddDays(1)));
    }

    [Fact]
    public void Terminate_twice_throws()
    {
        var employee = NewEmployee();
        employee.Terminate();

        employee.Status.ShouldBe(EmployeeStatus.Terminated);
        Should.Throw<DomainException>(employee.Terminate);
    }

    [Fact]
    public void Terminated_employee_cannot_be_updated()
    {
        var employee = NewEmployee();
        employee.Terminate();

        Should.Throw<DomainException>(() => employee.UpdateDetails("A", "B", "Dev", null, null));
    }

    [Fact]
    public void RequestLeave_counts_working_days_only()
    {
        var employee = NewEmployee();

        // Friday → next Monday = Fri + Mon = 2 working days
        var request = employee.RequestLeave(LeaveType.Annual, Friday, Friday.AddDays(3), "Trip", Monday);

        request.Days.ShouldBe(2);
        request.Status.ShouldBe(LeaveStatus.Pending);
        employee.LeaveRequests.ShouldContain(request);
    }

    [Fact]
    public void RequestLeave_on_weekend_only_throws()
    {
        var saturday = Friday.AddDays(1);
        Should.Throw<DomainException>(() =>
            NewEmployee().RequestLeave(LeaveType.Annual, saturday, saturday.AddDays(1), "Weekend", Monday));
    }

    [Fact]
    public void RequestLeave_in_the_past_throws()
    {
        Should.Throw<DomainException>(() =>
            NewEmployee().RequestLeave(LeaveType.Annual, Monday.AddDays(-1), Monday, "Late", Monday));
    }

    [Fact]
    public void RequestLeave_with_end_before_start_throws()
    {
        Should.Throw<DomainException>(() =>
            NewEmployee().RequestLeave(LeaveType.Annual, Friday, Monday.AddDays(1), "Backwards", Monday));
    }

    [Fact]
    public void RequestLeave_requires_a_reason()
    {
        Should.Throw<DomainException>(() =>
            NewEmployee().RequestLeave(LeaveType.Annual, Monday, Monday, "  ", Monday));
    }

    [Fact]
    public void RequestLeave_annual_beyond_balance_throws()
    {
        // 3 full weeks = 15 working days > 12
        Should.Throw<DomainException>(() =>
            NewEmployee().RequestLeave(LeaveType.Annual, Monday, Monday.AddDays(18), "Long trip", Monday));
    }

    [Fact]
    public void RequestLeave_counts_pending_annual_days_against_balance()
    {
        var employee = NewEmployee();
        employee.RequestLeave(LeaveType.Annual, Monday, Monday.AddDays(11), "First", Monday); // 10 working days pending

        var nextWeek = Monday.AddDays(14);
        Should.Throw<DomainException>(() =>
            employee.RequestLeave(LeaveType.Annual, nextWeek, nextWeek.AddDays(2), "Second", Monday)); // 3 more > 2 available
    }

    [Fact]
    public void RequestLeave_sick_does_not_use_annual_balance()
    {
        var request = NewEmployee().RequestLeave(LeaveType.Sick, Monday, Monday.AddDays(18), "Surgery", Monday);

        request.Days.ShouldBe(15);
    }

    [Fact]
    public void RequestLeave_overlapping_an_active_request_throws()
    {
        var employee = NewEmployee();
        employee.RequestLeave(LeaveType.Annual, Monday, Friday, "First", Monday);

        Should.Throw<DomainException>(() =>
            employee.RequestLeave(LeaveType.Sick, Friday, Friday, "Overlap", Monday));
    }

    [Fact]
    public void RequestLeave_may_reuse_dates_of_a_cancelled_request()
    {
        var employee = NewEmployee();
        employee.RequestLeave(LeaveType.Annual, Monday, Friday, "First", Monday).Cancel(employee.Id);

        Should.NotThrow(() => employee.RequestLeave(LeaveType.Annual, Monday, Friday, "Again", Monday));
    }

    [Fact]
    public void Terminated_employee_cannot_request_leave()
    {
        var employee = NewEmployee();
        employee.Terminate();

        Should.Throw<DomainException>(() =>
            employee.RequestLeave(LeaveType.Annual, Monday, Monday, "Leave", Monday));
    }
}
