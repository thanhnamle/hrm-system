using FluentValidation;
using HRM.Application.Common.Models;
using MediatR;

namespace HRM.Application.Features.Attendance.Queries;

/// <summary>The caller's own history, newest first.</summary>
public sealed record GetMyAttendanceQuery : IRequest<PagedResult<AttendanceRecordDto>>, IPagedQuery
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 31;
}

/// <summary>Who is in, out or on leave on <see cref="Date"/> (default today), within the caller's scope.</summary>
public sealed record GetTeamAttendanceQuery : IRequest<PagedResult<TeamAttendanceDto>>, IPagedQuery
{
    public DateOnly? Date { get; init; }
    public Guid? DepartmentId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetMyAttendanceValidator : PagedQueryValidator<GetMyAttendanceQuery>
{
    public GetMyAttendanceValidator() =>
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
}

public sealed class GetTeamAttendanceValidator : PagedQueryValidator<GetTeamAttendanceQuery>;
