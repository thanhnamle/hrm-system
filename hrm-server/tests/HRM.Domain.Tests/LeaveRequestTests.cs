using HRM.Domain.Common;
using HRM.Domain.Enums;
using static HRM.Domain.Tests.TestData;

namespace HRM.Domain.Tests;

public class LeaveRequestTests
{
    private static readonly Guid ManagerId = Guid.CreateVersion7();

    [Fact]
    public void Approve_annual_leave_deducts_balance_and_records_reviewer()
    {
        var employee = NewEmployee();
        var request = employee.RequestLeave(LeaveType.Annual, Monday, Friday, "Holiday", Monday);

        request.Approve(ManagerId, Now);

        request.Status.ShouldBe(LeaveStatus.Approved);
        request.ReviewedById.ShouldBe(ManagerId);
        request.ReviewedAt.ShouldBe(Now);
        employee.AnnualLeaveBalance.ShouldBe(12 - 5);
    }

    [Fact]
    public void Approve_sick_leave_keeps_annual_balance()
    {
        var employee = NewEmployee();
        employee.RequestLeave(LeaveType.Sick, Monday, Friday, "Flu", Monday).Approve(ManagerId, Now);

        employee.AnnualLeaveBalance.ShouldBe(12);
    }

    [Fact]
    public void Nobody_can_approve_their_own_request()
    {
        var employee = NewEmployee();
        var request = employee.RequestLeave(LeaveType.Annual, Monday, Monday, "Own", Monday);

        Should.Throw<DomainException>(() => request.Approve(employee.Id, Now));
        request.Status.ShouldBe(LeaveStatus.Pending);
    }

    [Fact]
    public void Only_pending_requests_can_be_reviewed()
    {
        var request = NewEmployee().RequestLeave(LeaveType.Annual, Monday, Monday, "Once", Monday);
        request.Approve(ManagerId, Now);

        Should.Throw<DomainException>(() => request.Approve(ManagerId, Now));
        Should.Throw<DomainException>(() => request.Reject(ManagerId, Now, "Too late"));
    }

    [Fact]
    public void Reject_requires_a_note_and_keeps_balance()
    {
        var employee = NewEmployee();
        var request = employee.RequestLeave(LeaveType.Annual, Monday, Friday, "Holiday", Monday);

        Should.Throw<DomainException>(() => request.Reject(ManagerId, Now, ""));
        request.Status.ShouldBe(LeaveStatus.Pending); // a failed reject must not change state

        request.Reject(ManagerId, Now, "Project deadline");
        request.Status.ShouldBe(LeaveStatus.Rejected);
        request.ReviewNote.ShouldBe("Project deadline");
        employee.AnnualLeaveBalance.ShouldBe(12);
    }

    [Fact]
    public void Only_the_requester_can_cancel()
    {
        var employee = NewEmployee();
        var request = employee.RequestLeave(LeaveType.Annual, Monday, Monday, "Mine", Monday);

        Should.Throw<DomainException>(() => request.Cancel(Guid.CreateVersion7()));

        request.Cancel(employee.Id);
        request.Status.ShouldBe(LeaveStatus.Cancelled);
    }

    [Fact]
    public void Approved_request_cannot_be_cancelled()
    {
        var employee = NewEmployee();
        var request = employee.RequestLeave(LeaveType.Annual, Monday, Monday, "Approved", Monday);
        request.Approve(ManagerId, Now);

        Should.Throw<DomainException>(() => request.Cancel(employee.Id));
    }
}
