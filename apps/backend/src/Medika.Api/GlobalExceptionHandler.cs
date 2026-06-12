using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Medika.Api;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Unhandled exception: {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        var (status, title) = ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            KeyNotFoundException        => (StatusCodes.Status404NotFound,     "Not found"),
            ArgumentException           => (StatusCodes.Status400BadRequest,   "Bad request"),
            _                           => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        // Report only genuine server faults to Sentry (skip expected 401/404/400 to avoid noise).
        // Explicit capture is required here: this handler marks the exception handled, and Serilog
        // replaces the logging providers, so Sentry's automatic capture wouldn't see it otherwise.
        if (status == StatusCodes.Status500InternalServerError)
            SentrySdk.CaptureException(ex);

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title  = title,
            Detail = ex is UnauthorizedAccessException or ArgumentException or KeyNotFoundException
                ? ex.Message
                : null,
        }, ct);

        return true;
    }
}
