using HRM.Application.Common.Security;
using HRM.Application.Features.Users.Commands;
using HRM.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.API.Controllers;

/// <summary>Login account management (Admin only).</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.UsersManage)]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserAccountDto>> GetUsers(CancellationToken cancellationToken) =>
        sender.Send(new GetUsersQuery(), cancellationToken);

    /// <summary>Creates the login for an existing employee. The login email is the employee's email.</summary>
    [HttpPost]
    public async Task<ActionResult<CreatedResponse>> Create(CreateUserAccountCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Created($"/api/users/{id}", new CreatedResponse(id));
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeRoleRequest body, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeUserRoleCommand(id, body.Role), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new LockUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new UnlockUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest body, CancellationToken cancellationToken)
    {
        await sender.Send(new ResetUserPasswordCommand(id, body.NewPassword), cancellationToken);
        return NoContent();
    }
}

public sealed record ChangeRoleRequest(string Role);

public sealed record ResetPasswordRequest(string NewPassword);
