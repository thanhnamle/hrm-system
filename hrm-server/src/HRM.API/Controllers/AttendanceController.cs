using HRM.Application.Common.Models;
using HRM.Application.Common.Security;
using HRM.Application.Features.Attendance;
using HRM.Application.Features.Attendance.Commands.CheckInOut;
using HRM.Application.Features.Attendance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.API.Controllers;

[ApiController]
[Route("api/attendance")]
public sealed class AttendanceController(ISender sender) : ControllerBase
{
    [HttpPost("check-in")]
    [Authorize(Policy = Permissions.AttendanceRecord)]
    public Task<AttendanceRecordDto> CheckIn(CancellationToken cancellationToken) =>
        sender.Send(new CheckInCommand(), cancellationToken);

    [HttpPost("check-out")]
    [Authorize(Policy = Permissions.AttendanceRecord)]
    public Task<AttendanceRecordDto> CheckOut(CancellationToken cancellationToken) =>
        sender.Send(new CheckOutCommand(), cancellationToken);

    [HttpGet("mine")]
    [Authorize(Policy = Permissions.AttendanceRecord)]
    public Task<PagedResult<AttendanceRecordDto>> GetMine([FromQuery] GetMyAttendanceQuery query, CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);

    /// <summary>Team view for one day: Admin/HR everyone, a Manager their departments.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.AttendanceView)]
    public Task<PagedResult<TeamAttendanceDto>> GetTeam([FromQuery] GetTeamAttendanceQuery query, CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);
}
