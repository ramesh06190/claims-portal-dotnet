using Microsoft.AspNetCore.Diagnostics;

namespace ClaimsPortal.Web.Middleware;

/// <summary>
/// Turns unhandled exceptions on /api routes into a ProblemDetails response that includes the
/// correlation ID but no exception details. Page requests fall through to the friendly error page.
/// </summary>
public sealed partial class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // UseExceptionHandler("/error") has already rewritten Request.Path to /error by the time
        // handlers run, so the path the caller asked for comes from the feature instead
        var originalPath = httpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? httpContext.Request.Path;
        if (!originalPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            && !originalPath.Equals("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        LogUnhandled(logger, exception, httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Something went wrong on our side",
                Detail = "Try again later. If the problem continues, contact support with the correlation ID.",
                Extensions = { ["correlationId"] = httpContext.TraceIdentifier },
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled API exception, correlation {CorrelationId}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string correlationId);
}
