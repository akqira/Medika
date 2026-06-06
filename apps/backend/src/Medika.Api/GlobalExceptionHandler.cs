using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Medika.Api;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            KeyNotFoundException        => (StatusCodes.Status404NotFound,     "Not found"),
            ArgumentException           => (StatusCodes.Status400BadRequest,   "Bad request"),
            _                           => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

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
