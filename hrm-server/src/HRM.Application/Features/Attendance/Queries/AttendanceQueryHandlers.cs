using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Models;
using HRM.Application.Common.Security;
using HRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Attendance.Queries;

public sealed class AttendanceQueryHandlers(IApplicationDbContext db, ICurrentUser currentUser, IClock clock) :
    IRequestHandler<GetMyAttendanceQuery, PagedResult<AttendanceRecordDto>>,
    IRequestHandler<GetTeamAttendanceQuery, PagedResult<TeamAttendanceDto>>
{
    public Task<PagedResult<AttendanceRecordDto>> Handle(GetMyAttendanceQuery request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.RequireEmployeeId();
        var query = db.AttendanceRecords.AsNoTracking().Where(a => a.EmployeeId == employeeId);

        if (request.From is { } from)
            query = query.Where(a => a.WorkDate >= from);
        if (request.To is { } to)
            query = query.Where(a => a.WorkDate <= to);

        return query
            .OrderByDescending(a => a.WorkDate)
            .Select(a => new AttendanceRecordDto(a.Id, a.WorkDate, a.CheckInAt, a.CheckOutAt))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public Task<PagedResult<TeamAttendanceDto>> Handle(GetTeamAttendanceQuery request, CancellationToken cancellationToken)
    {
        var date = request.Date ?? clock.Today;

        // Start from employees, not attendance rows, so people who did not check in show as absent.
        var employees = db.Employees.AsNoTracking()
            .ApplyScope(currentUser)
            .Where(e => e.Status == EmployeeStatus.Active);

        if (request.DepartmentId is { } departmentId)
            employees = employees.Where(e => e.DepartmentId == departmentId);

        return employees
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new TeamAttendanceDto(
                e.Id,
                e.EmployeeCode,
                e.FirstName + " " + e.LastName,
                e.Department.Name,
                db.AttendanceRecords.Where(a => a.EmployeeId == e.Id && a.WorkDate == date).Select(a => (DateTimeOffset?)a.CheckInAt).FirstOrDefault(),
                db.AttendanceRecords.Where(a => a.EmployeeId == e.Id && a.WorkDate == date).Select(a => a.CheckOutAt).FirstOrDefault(),
                e.LeaveRequests.Any(r => r.Status == LeaveStatus.Approved && r.StartDate <= date && r.EndDate >= date)))
            .ToPagedResultAsync(request, cancellationToken);
    }
}
