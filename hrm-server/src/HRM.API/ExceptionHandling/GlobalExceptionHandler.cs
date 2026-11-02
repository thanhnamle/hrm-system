using HRM.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace HRM.API.ExceptionHandling;

/// <summary>
/// Turns exceptions into RFC 7807 ProblemDetails responses. Internals (stack traces) never reach the client.
/// Validation (400), Unauthorized (401), Forbidden (403) and NotFound (404) mappings are added with their exception types.
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                // Only expected (4xx) errors expose their message; 500s stay generic.
                Detail = status < StatusCodes.Status500InternalServerError ? exception.Message : null,
            },
        });
    }
}
