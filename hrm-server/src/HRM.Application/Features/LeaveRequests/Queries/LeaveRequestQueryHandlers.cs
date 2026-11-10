using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Models;
using HRM.Application.Common.Security;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.LeaveRequests.Queries;

public sealed class LeaveRequestQueryHandlers(IApplicationDbContext db, ICurrentUser currentUser) :
    IRequestHandler<GetMyLeaveRequestsQuery, PagedResult<LeaveRequestDto>>,
    IRequestHandler<GetPendingLeaveRequestsQuery, PagedResult<LeaveRequestDto>>,
    IRequestHandler<GetLeaveRequestsQuery, PagedResult<LeaveRequestDto>>,
    IRequestHandler<GetMyLeaveBalanceQuery, LeaveBalanceDto>
{
    private IQueryable<LeaveRequest> Requests => db.LeaveRequests.AsNoTracking();

    public Task<PagedResult<LeaveRequestDto>> Handle(GetMyLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.RequireEmployeeId();
        var query = Requests.Where(r => r.EmployeeId == employeeId);

        if (request.Status is { } status)
            query = query.Where(r => r.Status == status);

        return query
            .OrderByDescending(r => r.StartDate).ThenByDescending(r => r.CreatedAt)
            .SelectDto(db.Employees)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public Task<PagedResult<LeaveRequestDto>> Handle(GetPendingLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.EmployeeId;

        return Requests
            .ApplyReviewScope(currentUser)
            .Where(r => r.Status == LeaveStatus.Pending)
            .Where(r => r.EmployeeId != employeeId) // own requests go to someone else
            .OrderBy(r => r.StartDate).ThenBy(r => r.CreatedAt)
            .SelectDto(db.Employees)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public Task<PagedResult<LeaveRequestDto>> Handle(GetLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = Requests;

        if (request.Status is { } status)
            query = query.Where(r => r.Status == status);
        if (request.Type is { } type)
            query = query.Where(r => r.Type == type);
        if (request.EmployeeId is { } employeeId)
            query = query.Where(r => r.EmployeeId == employeeId);
        if (request.DepartmentId is { } departmentId)
            query = query.Where(r => r.Employee.DepartmentId == departmentId);
        if (request.From is { } from)
            query = query.Where(r => r.EndDate >= from);
        if (request.To is { } to)
            query = query.Where(r => r.StartDate <= to);

        return query
            .OrderByDescending(r => r.StartDate).ThenByDescending(r => r.CreatedAt)
            .SelectDto(db.Employees)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<LeaveBalanceDto> Handle(GetMyLeaveBalanceQuery request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.RequireEmployeeId();

        var balance = await db.Employees.AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => new
                {
                    e.AnnualLeaveBalance,
                    Pending = e.LeaveRequests
                        .Where(r => r.Type == LeaveType.Annual && r.Status == LeaveStatus.Pending)
                        .Sum(r => r.Days),
                })
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        // Same formula Employee.RequestLeave uses to decide whether annual leave fits.
        return new LeaveBalanceDto(balance.AnnualLeaveBalance, balance.Pending, balance.AnnualLeaveBalance - balance.Pending);
    }
}
