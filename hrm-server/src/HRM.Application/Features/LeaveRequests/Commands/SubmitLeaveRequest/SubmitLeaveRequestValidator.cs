using FluentValidation;

namespace HRM.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>
/// Shape checks only. Business rules (past dates, overlap, balance, working days) are in
/// Employee.RequestLeave and come back as 422.
/// </summary>
public sealed class SubmitLeaveRequestValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    public const int MaxCalendarDays = 60;

    public SubmitLeaveRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty()
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("'End Date' must be on or after the start date.");
        RuleFor(x => x)
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber < MaxCalendarDays)
            .WithName("EndDate")
            .WithMessage($"A single request can cover at most {MaxCalendarDays} calendar days.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
