using System.Diagnostics;
using System.Globalization;

namespace Medika.Api.Middleware;

/// <summary>
/// Ensures the .NET API is only callable by the SvelteKit BFF (pattern ported from eGestion, ADR-008).
/// 1. X-API-KEY required on ALL endpoints, including /api/auth/login — no bypass list except health/swagger.
/// 2. X-Request-Timestamp (ISO 8601) required on all non-auth endpoints; rejected if > 5 min from UtcNow (anti-replay).
/// JWT validation itself stays with ASP.NET Core AddJwtBearer + FastEndpoints Roles().
///
/// Rejections are no longer opaque (ported from eGestion's RejectAsync):
/// - The exact gate that failed is logged server-side (Serilog → console/file/App Insights/Sentry).
///   Secrets are never logged — for the API key we log only missing vs mismatch, not its value.
/// - The 401 body stays generic for callers but carries a traceId; paste it in
///   App Insights → Logs: union requests, traces | where operation_Id == "&lt;traceId&gt;"
///   to land on the full transaction (headers, payload, rejection reason).
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-API-KEY";
    private const string TimestampHeaderName = "X-Request-Timestamp";
    private const int TimestampToleranceMinutes = 5;
    private readonly string _validApiKey;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        if (string.IsNullOrEmpty(providedKey))
        {
            await RejectAsync(context, "API key header missing");
            return;
        }
        if (!CryptographicEquals(providedKey, _validApiKey))
        {
            await RejectAsync(context, "API key mismatch (BFF API_SECRET out of sync with backend ApiSettings:ApiKey?)");
            return;
        }

        // Login: API key is sufficient — no timestamp needed (no JWT yet)
        if (path.StartsWithSegments("/api/auth/login"))
        {
            await _next(context);
            return;
        }

        // All other endpoints: require a fresh timestamp (anti-replay)
        if (!context.Request.Headers.TryGetValue(TimestampHeaderName, out v