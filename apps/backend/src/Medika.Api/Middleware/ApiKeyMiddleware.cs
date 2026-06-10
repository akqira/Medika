using System.Globalization;

namespace Medika.Api.Middleware;

/// <summary>
/// Ensures the .NET API is only callable by the SvelteKit BFF (pattern ported from eGestion, ADR-008).
/// 1. X-API-KEY required on ALL endpoints, including /api/auth/login — no bypass list except health/swagger.
/// 2. X-Request-Timestamp (ISO 8601) required on all non-auth endpoints; rejected if > 5 min from UtcNow (anti-replay).
/// JWT validation itself stays with ASP.NET Core AddJwtBearer + FastEndpoints Roles().
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-API-KEY";
    private const string TimestampHeaderName = "X-Request-Timestamp";
    private const int TimestampToleranceMinutes = 5;
    private readonly string _validApiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _validApiKey = configuration["ApiSettings:ApiKey"] ?? string.Empty;
        if (string.IsNullOrEmpty(_validApiKey))
            throw new InvalidOperationException("ApiSettings:ApiKey is not configured.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        // CORS preflight, health check and Swagger — no API key
        if (HttpMethods.IsOptions(context.Request.Method) ||
            path.StartsWithSegments("/api/health") ||
            path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        // Validate API key on every request (including auth endpoints)
        var providedKey = context.Request.Headers[ApiKeyHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(providedKey) ||
            !CryptographicEquals(providedKey, _validApiKey))
        {
            await Reject(context);
            return;
        }

        // Login: API key is sufficient — no timestamp needed (no JWT yet)
        if (path.StartsWithSegments("/api/auth/login"))
        {
            await _next(context);
            return;
        }

        // All other endpoints: require a fresh timestamp (anti-replay)
        if (!context.Request.Headers.TryGetValue(TimestampHeaderName, out var tsHeader) ||
            !DateTime.TryParse(tsHeader, null, DateTimeStyles.RoundtripKind, out var requestTime) ||
            Math.Abs((DateTime.UtcNow - requestTime.ToUniversalTime()).TotalMinutes) > TimestampToleranceMinutes)
        {
            await Reject(context);
            return;
        }

        await _next(context);
    }

    private static bool CryptographicEquals(string a, string b)
    {
        var bytesA = System.Text.Encoding.UTF8.GetBytes(a);
        var bytesB = System.Text.Encoding.UTF8.GetBytes(b);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }

    private static Task Reject(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
    }
}
