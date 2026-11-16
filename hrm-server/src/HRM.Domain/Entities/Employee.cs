using HRM.Domain.Common;
using HRM.Domain.Enums;

namespace HRM.Domain.Entities;

/// <summary>
/// Aggregate root for an employee: their record, job history, CV lines (qualifications, skills)
/// and leave requests. Load the collection a method works on before calling it, e.g.
/// <see cref="LeaveRequests"/> (pending + approved) before <see cref="RequestLeave"/>.
/// </summary>
public class Employee : AuditableEntity
{
    public const decimal DefaultAnnualLeaveDays = 12m;
    public const int MaxSkills = 30;
    public const int MaxSkillLength = 50;
    public const int MaxQualifications = 50;

    private readonly List<LeaveRequest> _leaveRequests = [];
    private readonly List<JobHistoryEntry> _jobHistory = [];
    private readonly List<Qualification> _qualifications = [];

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

    /// <summary>Free-text skill tags, e.g. "C#", "Negotiation". Stored as a PostgreSQL text[].</summary>
    public string[] Skills { get; private set; } = [];

    public IReadOnlyCollection<LeaveRequest> LeaveRequests => _leaveRequests.AsReadOnly();
    public IReadOnlyCollection<JobHistoryEntry> JobHistory => _jobHistory.AsReadOnly();
    public IReadOnlyCollection<Qualification> Qualifications => _qualifications.AsReadOnly();

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

        var employee = new Employee
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
        employee._jobHistory.Add(new JobHistoryEntry(employee, JobChangeType.Hired, hireDate, note: null));
        return employee;
    }

    /// <summary>Personal details only. Job changes go through <see cref="ChangeJob"/> so they are recorded.</summary>
    public void UpdateDetails(string firstName, string lastName, string? phone, DateOnly? dateOfBirth)
    {
        EnsureActive();
        if (dateOfBirth is { } dob && dob >= HireDate)
            throw new DomainException("Date of birth must be before the hire date.");

        FirstName = Guard.Required(firstName, "First name");
        LastName = Guard.Required(lastName, "Last name");
        Phone = Guard.Optional(phone);
        DateOfBirth = dateOfBirth;
    }

    public void ChangeEmail(string email)
    {
        EnsureActive();
        Email = Guard.Email(email);
    }

    /// <summary>
    /// New position and/or department, recorded in the job history: a department change is a
    /// transfer (with the position from then on), otherwise a position change. No change, no entry.
    /// Returns the new entry, or null.
    /// </summary>
    public JobHistoryEntry? ChangeJob(string position, Guid departmentId, DateOnly effectiveDate, string? note = null)
    {
        EnsureActive();
        if (departmentId == Guid.Empty)
            throw new DomainException("Department is required.");
        var newPosition = Guard.Required(position, "Position");
        if (effectiveDate < HireDate)
            throw new DomainException("A job change cannot take effect before the hire date.");

        var transferred = departmentId != DepartmentId;
        if (!transferred && newPosition == Position)
            return null;

        Position = newPosition;
        DepartmentId = departmentId;
        var entry = new JobHistoryEntry(
            this, transferred ? JobChangeType.Transferred : JobChangeType.PositionChanged, effectiveDate, note);
        _jobHistory.Add(entry);
        return entry;
    }

    /// <summary>
    /// Soft delete: the row stays for history, the employee can no longer log in or act.
    /// Pending leave requests are cancelled, so load them (<see cref="LeaveRequests"/>) first.
    /// </summary>
    public JobHistoryEntry Terminate(DateOnly effectiveDate, string? note = null)
    {
        if (Status == EmployeeStatus.Terminated)
            throw new DomainException("Employee is already terminated.");
        Status = EmployeeStatus.Terminated;

        foreach (var request in _leaveRequests.Where(r => r.Status == LeaveStatus.Pending))
            request.Withdraw();

        var entry = new JobHistoryEntry(this, JobChangeType.Terminated, effectiveDate, note);
        _jobHistory.Add(entry);
        return entry;
    }

    /// <summary>Replaces the skill list: trimmed, blanks dropped, duplicates (any case) removed.</summary>
    public void SetSkills(IEnumerable<string> skills)
    {
        EnsureActive();
        var cleaned = skills
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .DistinctBy(s => s.ToLowerInvariant())
            .ToArray();

        if (cleaned.Length > MaxSkills)
            throw new DomainException($"At most {MaxSkills} skills.");
        if (cleaned.Any(s => s.Length > MaxSkillLength))
            throw new DomainException($"Each skill can be at most {MaxSkillLength} characters.");

        Skills = cleaned;
    }

    /// <summary>Load <see cref="Qualifications"/> first (for the limit).</summary>
    public Qualification AddQualification(
        QualificationType type, string title, string organization, DateOnly? startDate, DateOnly? endDate, string? description)
    {
        EnsureActive();
        if (_qualifications.Count >= MaxQualifications)
            throw new DomainException($"At most {MaxQualifications} CV entries.");

        var qualification = new Qualification(Id);
        qualification.Set(type, title, organization, startDate, endDate, description);
        _qualifications.Add(qualification);
        return qualification;
    }

    /// <summary>Load <see cref="Qualifications"/> first.</summary>
    public void UpdateQualification(
        Guid qualificationId, QualificationType type, string title, string organization,
        DateOnly? startDate, DateOnly? endDate, string? description)
    {
        EnsureActive();
        FindQualification(qualificationId).Set(type, title, organization, startDate, endDate, description);
    }

    /// <summary>Load <see cref="Qualifications"/> first.</summary>
    public void RemoveQualification(Guid qualificationId)
    {
        EnsureActive();
        _qualifications.Remove(FindQualification(qualificationId));
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

    private Qualification FindQualification(Guid id) =>
        _qualifications.SingleOrDefault(q => q.Id == id)
        ?? throw new DomainException("This CV entry does not belong to the employee.");

    private void EnsureActive()
    {
        if (Status != EmployeeStatus.Active)
            throw new DomainException("Employee is terminated.");
    }
}
