using Microsoft.ApplicationInsights.DataContracts;
using System.Text;

namespace Medika.Api.Middleware;

/// <summary>
/// Captures HTTP call detail — method, path, status, headers and (conditionally) bodies —
/// and attaches it to:
///   1. The Application Insights RequestTelemetry custom properties.
///   2. Serilog (console + rolling file + App Insights traces sink).
/// Registered BEFORE ApiKeyMiddleware so rejected (401) calls are captured too.
///
/// ADR-001 redaction policy:
///   - Secret headers (Authorization, X-API-KEY, Cookie, Set-Cookie) are always [redacted].
///   - Bodies on sensitive routes (patients / consultations / invoices / finance / auth)
///     are NEVER logged, in any environment — they hold PII (incl. NSS), diagnoses,
///     financial data, and credentials.
///   - Bodies on other routes are captured only in Development (or when explicitly
///     enabled via "Logging:CaptureHttpBodies"); in Staging/Production they are suppressed.
/// </summary>
public class HttpDetailLoggingMiddleware
{
    // Application Insights caps custom property values at 8192 chars.
    private const int MaxBodyChars = 8000;

    private readonly RequestDelegate _next;
    private readonly ILogger<HttpDetailLoggingMiddleware> _logger;
    private readonly bool _enabled;
    private readonly bool _captureBodies;

    public HttpDetailLoggingMiddleware(
        RequestDelegate next,
        ILogger<HttpDetailLoggingMiddleware> logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _enabled = configuration.GetValue("Logging:CaptureHttpDetails", true);
        // Bodies captured by default only in Development; opt-in elsewhere (ADR-001).
        _captureBodies = configuration.GetValue("Logging:CaptureHttpBodies", environment.IsDevelopment());
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // A sensitive route's bodies are never logged, regardless of environment.
        var sensitive = HttpLogRedaction.IsSensitiveBodyPath(context.Request.Path.Value);

        // ----- Request side -----
        string requestBody = await CaptureRequestBodyAsync(context, sensitive);
        var requestHeaders = FormatHeaders(context.Request.Headers);

        // ----- Swap response stream so we can read the payload -----
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            string responseBody = await CaptureResponseBodyAsync(context, buffer, sensitive);

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            var responseHeaders = FormatHeaders(context.Response.Headers);

            var telemetry = context.Features.Get<RequestTelemetry>();
            if (telemetry is not null)
            {
                telemetry.Properties["RequestHeaders"] = requestHeaders;
                telemetry.Properties["RequestBody"] = requestBody;
                telemetry.Properties["ResponseHeaders"] = responseHeaders;
                telemetry.Properties["ResponseBody"] = responseBody;
            }

            _logger.LogInformation(
                "HTTP {Method} {Path}{Query} => {StatusCode}\n--- Request headers ---\n{RequestHeaders}\n--- Request body ---\n{RequestBody}\n--- Response body ---\n{ResponseBody}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.Value,
                context.Response.StatusCode,
                requestHeaders,
                requestBody,
                responseBody);
        }
    }

    private async Task<string> CaptureRequestBodyAsync(HttpContext context, bool sensitive)
    {
        if (sensitive) return HttpLogRedaction.SuppressedBody;
        if (!_captureBodies) return HttpLogRedaction.BodyCaptureDisabled;
        if (context.Request.ContentLength is not > 0 || !IsTextual(context.Request.ContentType))
            return string.Empty;

        context.Request.EnableBuffering();
        using var reader = new StreamReader(
            context.Request.Body, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = Truncate(await reader.ReadToEndAsync());
        context.Request.Body.Position = 0;
        return body;
    }

    private async Task<string> CaptureResponseBodyAsync(HttpContext context, MemoryStream buffer, bool sensitive)
    {
        if (sensitive) return HttpLogRedaction.SuppressedBody;
        if 