using FluentValidation;
using HRM.Application.Common.Models;
using HRM.Domain.Enums;
using MediatR;

namespace HRM.Application.Features.LeaveRequests.Queries;

/// <summary>The caller's own requests, newest first.</summary>
public sealed record GetMyLeaveRequestsQuery : IRequest<PagedResult<LeaveRequestDto>>, IPagedQuery
{
    public LeaveStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>Pending requests the caller can review (own departments for a Manager), oldest first.</summary>
public sealed record GetPendingLeaveRequestsQuery : IRequest<PagedResult<LeaveRequestDto>>, IPagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>All requests, for HR and Admin. <see cref="From"/>/<see cref="To"/> match requests overlapping that period.</summary>
public sealed record GetLeaveRequestsQuery : IRequest<PagedResult<LeaveRequestDto>>, IPagedQuery
{
    public LeaveStatus? Status { get; init; }
    public LeaveType? Type { get; init; }
    public Guid? EmployeeId { get; init; }
    public Guid? DepartmentId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record GetMyLeaveBalanceQuery : IRequest<LeaveBalanceDto>;

public sealed class GetMyLeaveRequestsValidator : PagedQueryValidator<GetMyLeaveRequestsQuery>;

public sealed class GetPendingLeaveRequestsValidator : PagedQueryValidator<GetPendingLeaveRequestsQuery>;

public sealed class GetLeaveRequestsValidator : PagedQueryValidator<GetLeaveRequestsQuery>
{
    public GetLeaveRequestsValidator() =>
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
}
