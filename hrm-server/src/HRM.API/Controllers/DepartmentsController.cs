using HRM.Application.Common.Security;
using HRM.Application.Features.Departments.Commands.AssignManager;
using HRM.Application.Features.Departments.Commands.CreateDepartment;
using HRM.Application.Features.Departments.Commands.DeleteDepartment;
using HRM.Application.Features.Departments.Commands.UpdateDepartment;
using HRM.Application.Features.Departments.Queries.GetDepartments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.API.Controllers;

[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.DepartmentsView)]
    public Task<IReadOnlyList<DepartmentDto>> GetDepartments(CancellationToken cancellationToken) =>
        sender.Send(new GetDepartmentsQuery(), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.DepartmentsManage)]
    public async Task<ActionResult<CreatedResponse>> Create(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Created($"/api/departments/{id}", new CreatedResponse(id));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.DepartmentsManage)]
    public async Task<IActionResult> Update(Guid id, UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.DepartmentsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteDepartmentCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/manager")]
    [Authorize(Policy = Permissions.DepartmentsManage)]
    public async Task<IActionResult> AssignManager(Guid id, AssignManagerRequest body, CancellationToken cancellationToken)
    {
        await sender.Send(new AssignManagerCommand(id, body.EmployeeId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/manager")]
    [Authorize(Policy = Permissions.DepartmentsManage)]
    public async Task<IActionResult> RemoveManager(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveManagerCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record AssignManagerRequest(Guid EmployeeId);
