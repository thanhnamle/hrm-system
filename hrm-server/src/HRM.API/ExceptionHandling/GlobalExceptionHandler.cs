using HRM.Application.Common.Exceptions;
using HRM.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HRM.API.ExceptionHandling;

/// <summary>
/// Turns exceptions into RFC 7807 ProblemDetails responses (mapping in docs/security.md §3).
/// Internals (stack traces, SQL) never reach the client.
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException e => (StatusCodes.Status400BadRequest, "Validation failed", e.Message),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, "Unauthorized", e.Message),
            ForbiddenAccessException e => (StatusCodes.Status403Forbidden, "Forbidden", e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflict", e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict",
                "The record was changed by someone else. Reload it and try again."),
            // Safety net for unique indexes when two requests race past the handler's own check.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "Conflict", "A record with the same unique value already exists."),
            DomainException e => (StatusCodes.Status422UnprocessableEntity, "Business rule violated", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", (string?)null),
        };

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        ProblemDetails problem = exception is ValidationException validation
            ? new HttpValidationProblemDetails(validation.Errors.ToDictionary())
            : new ProblemDetails();
        problem.Status = status;
        problem.Title = title;
        problem.Detail = detail;

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }
}
