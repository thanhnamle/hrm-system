using FluentValidation;

namespace HRM.Application.Features.LeaveRequests.Commands.ReviewLeaveRequest;

public sealed class ApproveLeaveRequestValidator : AbstractValidator<ApproveLeaveRequestCommand>
{
    public ApproveLeaveRequestValidator() => RuleFor(x => x.Note).MaximumLength(500);
}

public sealed class RejectLeaveRequestValidator : AbstractValidator<RejectLeaveRequestCommand>
{
    public RejectLeaveRequestValidator() => RuleFor(x => x.Note).NotEmpty().MaximumLength(500);
}
