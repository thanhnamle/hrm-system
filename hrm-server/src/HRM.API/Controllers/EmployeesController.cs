using HRM.Application.Common.Models;
using HRM.Application.Common.Security;
using HRM.Application.Features.Employees;
using HRM.Application.Features.Employees.Commands.CreateEmployee;
using HRM.Application.Features.Employees.Commands.TerminateEmployee;
using HRM.Application.Features.Employees.Commands.UpdateEmployee;
using HRM.Application.Features.Employees.Queries.GetEmployeeById;
using HRM.Application.Features.Employees.Queries.GetEmployees;
using HRM.Application.Features.Employees.Profile;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

    /// <summary>The body is optional: <c>{ "note": "Resigned" }</c>, kept in the job history.</summary>
    [HttpPost("{id:guid}/terminate")]
    [Authorize(Policy = Permissions.EmployeesTerminate)]
    public async Task<IActionResult> Terminate(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] NoteRequest? body,
        CancellationToken cancellationToken)
    {
        await sender.Send(new TerminateEmployeeCommand(id, body?.Note), cancellationToken);
        return NoContent();
    }

    // --- Profile: account role, job history, skills, qualifications, CV ---

    [HttpGet("{id:guid}/profile")]
    [Authorize(Policy = Permissions.EmployeesView)]
    public Task<EmployeeProfileDto> GetProfile(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new GetEmployeeProfileQuery(id), cancellationToken);

    [HttpPut("{id:guid}/skills")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    public async Task<IActionResult> UpdateSkills(Guid id, SkillsRequest body, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateSkillsCommand(id, body.Skills), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/qualifications")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    public async Task<ActionResult<CreatedResponse>> AddQualification(
        Guid id, AddQualificationCommand command, CancellationToken cancellationToken)
    {
        var qualificationId = await sender.Send(command with { EmployeeId = id }, cancellationToken);
        return Created($"/api/employees/{id}/profile", new CreatedResponse(qualificationId));
    }

    [HttpPut("{id:guid}/qualifications/{qualificationId:guid}")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    public async Task<IActionResult> UpdateQualification(
        Guid id, Guid qualificationId, UpdateQualificationCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { EmployeeId = id, QualificationId = qualificationId }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/qualifications/{qualificationId:guid}")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    public async Task<IActionResult> DeleteQualification(Guid id, Guid qualificationId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteQualificationCommand(id, qualificationId), cancellationToken);
        return NoContent();
    }

    /// <summary>multipart/form-data with a "file" field: a PDF up to 5 MB. Replaces the current CV.</summary>
    [HttpPut("{id:guid}/cv")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<CvInfoDto> UploadCv(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        // Content type and extension from the browser are not trusted: the domain checks the bytes.
        using var buffer = new MemoryStream((int)Math.Min(file.Length, EmployeeDocument.MaxSizeBytes + 1));
        await file.CopyToAsync(buffer, cancellationToken);
        return await sender.Send(new UploadCvCommand(id, file.FileName, buffer.ToArray()), cancellationToken);
    }

    [HttpGet("{id:guid}/cv")]
    [Authorize(Policy = Permissions.EmployeesView)]
    public async Task<IActionResult> DownloadCv(Guid id, CancellationToken cancellationToken)
    {
        var cv = await sender.Send(new GetCvFileQuery(id), cancellationToken);
        Response.Headers.XContentTypeOptions = "nosniff"; // never let the browser guess another type
        return File(cv.Content, cv.ContentType, cv.FileName); // Content-Disposition: attachment
    }

    [HttpDelete("{id:guid}/cv")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    public async Task<IActionResult> DeleteCv(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCvCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>5 MB file plus room for the multipart envelope; anything larger is refused before it is read.</summary>
    private const int MaxUploadBytes = EmployeeDocument.MaxSizeBytes + 64 * 1024;
}

public sealed record CreatedResponse(Guid Id);

public sealed record NoteRequest(string? Note);

public sealed record SkillsRequest(IReadOnlyList<string> Skills);
