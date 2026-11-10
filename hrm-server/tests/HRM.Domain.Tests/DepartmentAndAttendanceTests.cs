using HRM.Domain.Common;
using HRM.Domain.Entities;
using static HRM.Domain.Tests.TestData;

namespace HRM.Domain.Tests;

public class DepartmentTests
{
    [Fact]
    public void Create_normalizes_code_and_requires_name()
    {
        Department.Create(" eng ", "Engineering").Code.ShouldBe("ENG");
        Should.Throw<DomainException>(() => Department.Create("ENG", ""));
    }

    [Fact]
    public void AssignManager_sets_manager()
    {
        var department = Department.Create("ENG", "Engineering");
        var manager = NewEmployee(departmentId: department.Id);

        department.AssignManager(manager);

        department.ManagerId.ShouldBe(manager.Id);
    }

    [Fact]
    public void Terminated_employee_cannot_be_manager()
    {
        var department = Department.Create("ENG", "Engineering");
        var manager = NewEmployee(departmentId: department.Id);
        manager.Terminate();

        Should.Throw<DomainException>(() => department.AssignManager(manager));
    }

    [Fact]
    public void Manager_must_belong_to_the_department()
    {
        var department = Department.Create("ENG", "Engineering");
        var outsider = NewEmployee(departmentId: Guid.CreateVersion7());

        Should.Throw<DomainException>(() => department.AssignManager(outsider));
        department.ManagerId.ShouldBeNull();
    }
}

public class AttendanceRecordTests
{
    [Fact]
    public void CheckIn_then_CheckOut_records_both_times()
    {
        var record = AttendanceRecord.CheckIn(NewEmployee(), Monday, Now);

        record.CheckOut(Now.AddHours(8));

        record.WorkDate.ShouldBe(Monday);
        record.CheckOutAt.ShouldBe(Now.AddHours(8));
        record.IsCheckedOut.ShouldBeTrue();
    }

    [Fact]
    public void CheckOut_must_be_after_check_in()
    {
        var record = AttendanceRecord.CheckIn(NewEmployee(), Monday, Now);

        Should.Throw<DomainException>(() => record.CheckOut(Now));
    }

    [Fact]
    public void CheckOut_twice_throws()
    {
        var record = AttendanceRecord.CheckIn(NewEmployee(), Monday, Now);
        record.CheckOut(Now.AddHours(8));

        Should.Throw<DomainException>(() => record.CheckOut(Now.AddHours(9)));
    }

    [Fact]
    public void Terminated_employee_cannot_check_in()
    {
        var employee = NewEmployee();
        employee.Terminate();

        Should.Throw<DomainException>(() => AttendanceRecord.CheckIn(employee, Monday, Now));
    }
}

public class WorkingDaysTests
{
    [Fact]
    public void Test_anchor_date_is_a_monday() => Monday.DayOfWeek.ShouldBe(DayOfWeek.Monday);

    [Theory]
    [InlineData(0, 0, 1)]   // Mon
    [InlineData(0, 4, 5)]   // Mon–Fri
    [InlineData(4, 7, 2)]   // Fri–Mon
    [InlineData(5, 6, 0)]   // Sat–Sun
    [InlineData(0, 13, 10)] // two full weeks
    public void Count_skips_weekends(int fromOffset, int toOffset, int expected) =>
        WorkingDays.Count(Monday.AddDays(fromOffset), Monday.AddDays(toOffset)).ShouldBe(expected);
}
