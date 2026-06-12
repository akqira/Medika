using Microsoft.ApplicationInsights.DataContracts;
using System.Text;

namespace Medika.Api.Middleware;

/// <summary>
/// Captures full HTTP call detail — request/response headers (incl. Authorization bearer),
/// request payload and response payload — and attaches them to:
///   1. The Application Insights RequestTelemetry (visible per-request in the Azure portal
///      under "Custom Properties" of each request).
///   2. Serilog (console + rolling file + App Insights traces sink).
/// Enabled by default; disable with "Logging:CaptureHttpDetails": false.
/// Registered BEFORE ApiKeyMiddleware so rejected (401) calls are captured too.
/// </summary>
public class HttpDetailLoggingMiddleware
{
    // Application Insights caps custom property values at 8192 chars.
    private const int MaxBodyChars = 8000;

    private readonly RequestDelegate _next;
    private readonly ILogger<HttpDetailLoggingMiddleware> _logger;
    private readonly bool _enabled;

    public HttpDetailLoggingMiddleware(
        RequestDelegate next,
        ILogger<HttpDetailLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _enabled = configuration.GetValue("Logging:CaptureHttpDetails", true);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // ----- Request side -----
        string requestBody = string.Empty;
        if (context.Request.ContentLength > 0 && IsTextual(context.Request.ContentType))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body, Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            requestBody = Truncate(await reader.ReadToEndAsync());
            context.Request.Body.Position = 0;
        }

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
            string responseBody;
            buffer.Position = 0;
            if (IsTextual(context.Response.ContentType))
            {
                using var respReader = new StreamReader(buffer, Encoding.UTF8, false, 1024, leaveOpen: true);
                responseBody = Truncate(await respReader.ReadToEndAsync());
            }
            else
            {
                responseBody = $"<{context.Response.ContentType ?? "no content-type"}: {buffer.Length} bytes>";
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            var responseHeaders = FormatHeaders(context.Response.Headers);

            // Attach to the App Insights request telemetry for this call.
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

    private static bool ShouldSkip(PathString path) =>
        path.StartsWithSegments("/swagger");

    private static bool IsTextual(string? contentType) =>
        contentType is not null &&
        (contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
         || contentType.Contains("text", StringComparison.OrdinalIgnoreCase)
         || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
         || contentType.Contains("urlencoded", StringComparison.OrdinalIgnoreCase));

    private static string FormatHeaders(IHeaderDictionary headers) =>
        string.Join("\n", headers.Select(h => $"{h.Key}: {h.Value}"));

    private static string Truncate(string value) =>
        value.Length <= MaxBodyChars ? value : value[..MaxBodyChars] + "…[truncated]";
}
