using HRM.Application.Common.Exceptions;
using HRM.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HRM.API.ExceptionHandling;

/// <summary>
/// Turns exceptions into RFC 7807 ProblemDetails responses (mapping in docs/security.md §3).
/// Internals (stack traces) never reach the client.
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        ProblemDetails problem = exception is ValidationException validation
            ? new HttpValidationProblemDetails(validation.Errors.ToDictionary())
            : new ProblemDetails();
        problem.Status = status;
        problem.Title = title;
        // Only expected (4xx) errors expose their message; 500s stay generic.
        problem.Detail = status < StatusCodes.Status500InternalServerError ? exception.Message : null;

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }
}
