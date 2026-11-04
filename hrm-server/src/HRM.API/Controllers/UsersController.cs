using HRM.Application.Common.Security;
using HRM.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.API.Controllers;

/// <summary>Account management (Admin only). Create, change role and lock come on Day 9.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.UsersManage)]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserAccountDto>> GetUsers(CancellationToken cancellationToken) =>
        sender.Send(new GetUsersQuery(), cancellationToken);
}
