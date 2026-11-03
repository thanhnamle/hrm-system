using HRM.Domain.Common;
using HRM.Domain.Enums;

namespace HRM.Domain.Entities;

/// <summary>
/// Aggregate root for an employee and their leave requests.
/// Load <see cref="LeaveRequests"/> (pending + approved) before calling <see cref="RequestLeave"/>.
/// </summary>
public class Employee : AuditableEntity
{
    public const decimal DefaultAnnualLeaveDays = 12m;

    private readonly List<LeaveRequest> _leaveRequests = [];

    private Employee() { } // EF Core

    public string EmployeeCode { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public DateOnly HireDate { get; private set; }
    public string Position { get; private set; } = string.Empty;

    public Guid DepartmentId { get; private set; }
    public Department Department { get; private set; } = null!;

    public EmployeeStatus Status { get; private set; }
    public decimal AnnualLeaveBalance { get; private set; }

    public IReadOnlyCollection<LeaveRequest> LeaveRequests => _leaveRequests.AsReadOnly();

    public bool IsActive => Status == EmployeeStatus.Active;

    public static Employee Hire(
        string employeeCode,
        string firstName,
        string lastName,
        string email,
        string position,
        Guid departmentId,
        DateOnly hireDate,
        string? phone = null,
        DateOnly? dateOfBirth = null)
    {
        if (departmentId == Guid.Empty)
            throw new DomainException("Department is required.");
        if (dateOfBirth is { } dob && dob >= hireDate)
            throw new DomainException("Date of birth must be before the hire date.");

        return new Employee
        {
            EmployeeCode = Guard.Required(employeeCode, "Employee code").ToUpperInvariant(),
            FirstName = Guard.Required(firstName, "First name"),
            LastName = Guard.Required(lastName, "Last name"),
            Email = Guard.Email(email),
            Position = Guard.Required(position, "Position"),
            DepartmentId = departmentId,
            HireDate = hireDate,
            Phone = Guard.Optional(phone),
            DateOfBirth = dateOfBirth,
            Status = EmployeeStatus.Active,
            AnnualLeaveBalance = DefaultAnnualLeaveDays,
        };
    }

    public void UpdateDetails(string firstName, string lastName, string position, string? phone, DateOnly? dateOfBirth)
    {
        EnsureActive();
        if (dateOfBirth is { } dob && dob >= HireDate)
            throw new DomainException("Date of birth must be before the hire date.");

        FirstName = Guard.Required(firstName, "First name");
        LastName = Guard.Required(lastName, "Last name");
        Position = Guard.Required(position, "Position");
        Phone = Guard.Optional(phone);
        DateOfBirth = dateOfBirth;
    }

    public void ChangeEmail(string email)
    {
        EnsureActive();
        Email = Guard.Email(email);
    }

    public void TransferTo(Guid departmentId)
    {
        EnsureActive();
        if (departmentId == Guid.Empty)
            throw new DomainException("Department is required.");
        DepartmentId = departmentId;
    }

    /// <summary>Soft delete: the row stays for history, the employee can no longer log in or act.</summary>
    public void Terminate()
    {
        if (Status == EmployeeStatus.Terminated)
            throw new DomainException("Employee is already terminated.");
        Status = EmployeeStatus.Terminated;
    }

    public LeaveRequest RequestLeave(LeaveType type, DateOnly startDate, DateOnly endDate, string reason, DateOnly today)
    {
        EnsureActive();

        if (endDate < startDate)
            throw new DomainException("End date must be on or after the start date.");
        if (startDate < today)
            throw new DomainException("Leave cannot start in the past.");

        var days = WorkingDays.Count(startDate, endDate);
        if (days == 0)
            throw new DomainException("The selected period contains no working days.");

        if (_leaveRequests.Any(r => r.IsActive && r.Overlaps(startDate, endDate)))
            throw new DomainException("The period overlaps an existing pending or approved leave request.");

        if (type == LeaveType.Annual)
        {
            var pendingDays = _leaveRequests
                .Where(r => r.Type == LeaveType.Annual && r.Status == LeaveStatus.Pending)
                .Sum(r => r.Days);
            var available = AnnualLeaveBalance - pendingDays;
            if (days > available)
                throw new DomainException($"Not enough annual leave: requested {days} day(s), available {available}.");
        }

        var request = new LeaveRequest(this, type, startDate, endDate, days, Guard.Required(reason, "Reason"));
        _leaveRequests.Add(request);
        return request;
    }

    internal void DeductAnnualLeave(decimal days)
    {
        if (days > AnnualLeaveBalance)
            throw new DomainException($"Not enough annual leave: {days} day(s) needed, balance is {AnnualLeaveBalance}.");
        AnnualLeaveBalance -= days;
    }

    private void EnsureActive()
    {
        if (Status != EmployeeStatus.Active)
            throw new DomainException("Employee is terminated.");
    }
}
