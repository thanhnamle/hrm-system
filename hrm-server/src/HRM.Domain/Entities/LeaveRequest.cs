using HRM.Domain.Common;
using HRM.Domain.Enums;

namespace HRM.Domain.Entities;

/// <summary>
/// Created only through <see cref="Entities.Employee.RequestLeave"/>. Which manager may review it (data scope)
/// is checked in the Application layer; the rules here hold for every reviewer.
/// </summary>
public class LeaveRequest : AuditableEntity
{
    private LeaveRequest() { } // EF Core

    internal LeaveRequest(Employee employee, LeaveType type, DateOnly startDate, DateOnly endDate, decimal days, string reason)
    {
        Employee = employee;
        EmployeeId = employee.Id;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Days = days;
        Reason = reason;
        Status = LeaveStatus.Pending;
    }

    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = null!;

    public LeaveType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    /// <summary>Working days (Mon–Fri) in the period.</summary>
    public decimal Days { get; private set; }

    public string Reason { get; private set; } = string.Empty;
    public LeaveStatus Status { get; private set; }

    public Guid? ReviewedById { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }

    /// <summary>Pending or approved requests block overlapping periods.</summary>
    public bool IsActive => Status is LeaveStatus.Pending or LeaveStatus.Approved;

    public bool Overlaps(DateOnly start, DateOnly end) => StartDate <= end && start <= EndDate;

    /// <summary>Requires <see cref="Employee"/> to be loaded, because approving annual leave deducts the balance.</summary>
    public void Approve(Guid reviewerId, DateTimeOffset now, string? note = null)
    {
        EnsureReviewable(reviewerId);
        if (Employee is null)
            throw new InvalidOperationException("Load LeaveRequest.Employee before approving.");

        if (Type == LeaveType.Annual)
            Employee.DeductAnnualLeave(Days);

        Status = LeaveStatus.Approved;
        SetReview(reviewerId, now, Guard.Optional(note));
    }

    public void Reject(Guid reviewerId, DateTimeOffset now, string note)
    {
        EnsureReviewable(reviewerId);
        var reviewNote = Guard.Required(note, "Rejection note"); // validate before changing any state
        Status = LeaveStatus.Rejected;
        SetReview(reviewerId, now, reviewNote);
    }

    public void Cancel(Guid requestedByEmployeeId)
    {
        if (requestedByEmployeeId != EmployeeId)
            throw new DomainException("Only the employee who made the request can cancel it.");
        EnsurePending();
        Status = LeaveStatus.Cancelled;
    }

    /// <summary>Cancelled by the system, e.g. when the employee is terminated.</summary>
    internal void Withdraw()
    {
        EnsurePending();
        Status = LeaveStatus.Cancelled;
    }

    private void EnsureReviewable(Guid reviewerId)
    {
        if (reviewerId == EmployeeId)
            throw new DomainException("You cannot review your own leave request.");
        EnsurePending();
    }

    private void EnsurePending()
    {
        if (Status != LeaveStatus.Pending)
            throw new DomainException($"Only pending requests can be changed; this one is {Status}.");
    }

    private void SetReview(Guid reviewerId, DateTimeOffset now, string? note)
    {
        ReviewedById = reviewerId;
        ReviewedAt = now;
        ReviewNote = note;
    }
}
