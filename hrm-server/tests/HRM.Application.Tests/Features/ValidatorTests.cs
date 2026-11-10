using HRM.Application.Features.Departments.Commands.CreateDepartment;
using HRM.Application.Features.Employees.Commands.CreateEmployee;
using HRM.Application.Features.Employees.Queries.GetEmployees;
using HRM.Application.Features.LeaveRequests.Commands.ReviewLeaveRequest;
using HRM.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;
using HRM.Domain.Enums;

namespace HRM.Application.Tests.Features;

public class ValidatorTests
{
    private static readonly DateOnly Monday = new(2030, 1, 7);

    private static readonly CreateEmployeeCommand ValidEmployee = new(
        "An", "Nguyen", "an.nguyen@hrm.local", "Developer", Guid.CreateVersion7(), Monday, "+84 912 345 678", new DateOnly(1995, 5, 1));

    [Fact]
    public void Valid_employee_passes() =>
        new CreateEmployeeValidator().Validate(ValidEmployee).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("FirstName")]
    [InlineData("Email")]
    [InlineData("Phone")]
    [InlineData("DepartmentId")]
    [InlineData("DateOfBirth")]
    public void Invalid_employee_field_is_reported(string field)
    {
        var command = field switch
        {
            "FirstName" => ValidEmployee with { FirstName = "" },
            "Email" => ValidEmployee with { Email = "not-an-email" },
            "Phone" => ValidEmployee with { Phone = "call me" },
            "DepartmentId" => ValidEmployee with { DepartmentId = Guid.Empty },
            _ => ValidEmployee with { DateOfBirth = Monday.AddDays(1) }, // born after hire date
        };

        new CreateEmployeeValidator().Validate(command).Errors.ShouldContain(e => e.PropertyName == field);
    }

    [Theory]
    [InlineData(1, 20, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void Paging_limits(int page, int pageSize, bool valid) =>
        new GetEmployeesValidator().Validate(new GetEmployeesQuery { Page = page, PageSize = pageSize }).IsValid.ShouldBe(valid);

    [Theory]
    [InlineData("ENG", true)]
    [InlineData("R-and_D2", true)]
    [InlineData("Q A", false)]
    [InlineData("", false)]
    public void Department_code_format(string code, bool valid) =>
        new CreateDepartmentValidator().Validate(new CreateDepartmentCommand(code, "Name", null)).IsValid.ShouldBe(valid);

    [Fact]
    public void Leave_end_before_start_is_rejected() =>
        new SubmitLeaveRequestValidator()
            .Validate(new SubmitLeaveRequestCommand(LeaveType.Annual, Monday, Monday.AddDays(-1), "Trip"))
            .Errors.ShouldContain(e => e.PropertyName == "EndDate");

    [Fact]
    public void Leave_longer_than_limit_is_rejected() =>
        new SubmitLeaveRequestValidator()
            .Validate(new SubmitLeaveRequestCommand(LeaveType.Unpaid, Monday, Monday.AddDays(SubmitLeaveRequestValidator.MaxCalendarDays), "Sabbatical"))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void Leave_type_must_be_known() =>
        new SubmitLeaveRequestValidator()
            .Validate(new SubmitLeaveRequestCommand((LeaveType)99, Monday, Monday, "Trip"))
            .Errors.ShouldContain(e => e.PropertyName == "Type");

    [Fact]
    public void Rejection_needs_a_note()
    {
        new RejectLeaveRequestValidator().Validate(new RejectLeaveRequestCommand(Guid.CreateVersion7(), "")).IsValid.ShouldBeFalse();
        new RejectLeaveRequestValidator().Validate(new RejectLeaveRequestCommand(Guid.CreateVersion7(), "Busy week")).IsValid.ShouldBeTrue();
    }
}
