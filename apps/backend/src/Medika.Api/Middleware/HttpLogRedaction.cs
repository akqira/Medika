namespace Medika.Api.Middleware;

/// <summary>
/// Pure redaction policy for HTTP detail logging (ADR-001). Kept dependency-free
/// (plain strings, no ASP.NET types) so it is trivially unit-testable and so the
/// rules live in exactly one place.
///
/// Two protections:
///   1. Secret headers (Authorization, X-API-KEY, Cookie, Set-Cookie) are NEVER logged.
///   2. Request/response BODIES on sensitive routes (patient PII incl. NSS, clinical
///      diagnoses, financial data, login credentials) are NEVER logged, in any
///      environment.
/// </summary>
public static class HttpLogRedaction
{
    public const string Redacted = "[redacted]";
    public const string SuppressedBody = "[redacted: sensitive route]";
    public const string BodyCaptureDisabled = "[body capture disabled]";

    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "X-API-KEY",
        "Cookie",
        "Set-Cookie",
    };

    // Route prefixes whose bodies carry PII / clinical / financial / credential data.
    private static readonly string[] SensitiveBodyPrefixes =
    {
        "/api/patients",
        "/api/consultations",
        "/api/invoices",
        "/api/finance",
        "/api/auth",
    };

    public static bool IsSensitiveHeader(string name) => SensitiveHeaders.Contains(name);

    /// <summary>Returns the loggable value for a header — the real value, or [redacted] for secrets.</summary>
    public static string RedactHeaderValue(string name, string value) =>
        IsSensitiveHeader(name) ? Redacted : value;

    /// <summary>True when the path's body must never be written to logs (matches prefix or prefix/...).</summary>
    public static bool IsSensitiveBodyPath(string? path) =>
        !string.IsNullOrEmpty(path) && SensitiveBodyPrefixes.Any(p =>
            path.Equals(p, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
}
