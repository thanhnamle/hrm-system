using HRM.Application.Features.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRM.API.Controllers;

/// <summary>Any signed-in user; the content depends on the role.</summary>
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<DashboardDto> Get(CancellationToken cancellationToken) =>
        sender.Send(new GetDashboardQuery(), cancellationToken);
}
