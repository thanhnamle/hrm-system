using HRM.Application.Common.Models;
using HRM.Application.Common.Security;
using HRM.Application.Features.LeaveRequests;
using HRM.Application.Features.LeaveRequests.Commands.CancelLeaveRequest;
using HRM.Application.Features.LeaveRequests.Commands.ReviewLeaveRequest;
using HRM.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;
using HRM.Application.Features.LeaveRequests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HRM.API.Controllers;

[ApiController]
[Route("api/leave-requests")]
public sealed class LeaveRequestsController(ISender sender) : ControllerBase
{
    // --- Own requests (Leave.Request) ---

    [HttpGet("mine")]
    [Authorize(Policy = Permissions.LeaveRequest)]
    public Task<PagedResult<LeaveRequestDto>> GetMine([FromQuery] GetMyLeaveRequestsQuery query, CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);

    [HttpGet("balance")]
    [Authorize(Policy = Permissions.LeaveRequest)]
    public Task<LeaveBalanceDto> GetBalance(CancellationToken cancellationToken) =>
        sender.Send(new GetMyLeaveBalanceQuery(), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.LeaveRequest)]
    public async Task<ActionResult<CreatedResponse>> Submit(SubmitLeaveRequestCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Created($"/api/leave-requests/{id}", new CreatedResponse(id));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.LeaveRequest)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelLeaveRequestCommand(id), cancellationToken);
        return NoContent();
    }

    // --- Reviewing (Leave.Approve) ---

    [HttpGet("pending")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public Task<PagedResult<LeaveRequestDto>> GetPending([FromQuery] GetPendingLeaveRequestsQuery query, CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);

    /// <summary>The body is optional: <c>{ "note": "..." }</c>.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReviewNoteRequest? body,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ApproveLeaveRequestCommand(id, body?.Note), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Permissions.LeaveApprove)]
    public async Task<IActionResult> Reject(Guid id, ReviewNoteRequest body, CancellationToken cancellationToken)
    {
        await sender.Send(new RejectLeaveRequestCommand(id, body.Note ?? string.Empty), cancellationToken);
        return NoContent();
    }

    // --- Everyone's requests (Leave.ViewAll) ---

    [HttpGet]
    [Authorize(Policy = Permissions.LeaveViewAll)]
    public Task<PagedResult<LeaveRequestDto>> GetAll([FromQuery] GetLeaveRequestsQuery query, CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);
}

public sealed record ReviewNoteRequest(string? Note);
