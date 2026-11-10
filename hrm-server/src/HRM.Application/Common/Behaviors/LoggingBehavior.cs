using System.Diagnostics;
using HRM.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HRM.Application.Common.Behaviors;

/// <summary>
/// Logs every command and query with the caller and duration. Request contents are never logged,
/// because they can hold passwords or personal data.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const double SlowRequestMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var start = Stopwatch.GetTimestamp();

        try
        {
            var response = await next(cancellationToken);

            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            logger.Log(elapsedMs > SlowRequestMs ? LogLevel.Warning : LogLevel.Information,
                "{RequestName} by user {UserId} completed in {ElapsedMs:0} ms", requestName, currentUser.UserId, elapsedMs);
            return response;
        }
        catch (Exception ex)
        {
            // The exception handler logs 5xx details; this line records which use case failed and how.
            logger.LogInformation("{RequestName} by user {UserId} failed after {ElapsedMs:0} ms with {ErrorType}",
                requestName, currentUser.UserId, Stopwatch.GetElapsedTime(start).TotalMilliseconds, ex.GetType().Name);
            throw;
        }
    }
}
