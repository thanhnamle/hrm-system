using HRM.Application.Common.Models;
using HRM.Application.Common.Security;
using HRM.Application.Features.Employees;
using HRM.Application.Features.Employees.Commands.CreateEmployee;
using HRM.Application.Features.Employees.Commands.TerminateEmployee;
using HRM.Application.Features.Employees.Commands.UpdateEmployee;
using HRM.Application.Features.Employees.Queries.GetEmployeeById;
using HRM.Application.Features.Employees.Queries.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.API.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(ISender sender) : ControllerBase
{
    /// <summary>Admin/HR see everyone, a Manager their departments, an Employee only themselves.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.EmployeesView)]
    public Task<PagedResult<EmployeeListItemDto>> GetEmployees([FromQuery] GetEmployeesQuery query, CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeesView)]
    public Task<EmployeeDetailDto> GetEmployee(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new GetEmployeeByIdQuery(id), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeesCreate)]
    public async Task<ActionResult<CreatedResponse>> Create(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetEmployee), new { id }, new CreatedResponse(id));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { Id = id }, cancellationToken); // the route id wins over any id in the body
        return NoContent();
    }

    [HttpPost("{id:guid}/terminate")]
    [Authorize(Policy = Permissions.EmployeesTerminate)]
    public async Task<IActionResult> Terminate(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new TerminateEmployeeCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreatedResponse(Guid Id);
