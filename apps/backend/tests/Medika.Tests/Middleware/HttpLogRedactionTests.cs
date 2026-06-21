using Medika.Api.Middleware;
using Xunit;

namespace Medika.Tests.Middleware;

/// <summary>
/// ADR-001 — secret headers are never logged, and bodies on sensitive routes
/// (PII / clinical / financial / credential) are never logged.
/// </summary>
public class HttpLogRedactionTests
{
    [Theory]
    [InlineData("Authorization")]
    [InlineData("authorization")]
    [InlineData("X-API-KEY")]
    [InlineData("x-api-key")]
    [InlineData("Cookie")]
    [InlineData("Set-Cookie")]
    public void Secret_headers_are_redacted(string header)
    {
        Assert.True(HttpLogRedaction.IsSensitiveHeader(header));
        Assert.Equal(HttpLogRedaction.Redacted, HttpLogRedaction.RedactHeaderValue(header, "super-secret-value"));
    }

    [Theory]
    [InlineData("Content-Type", "application/json")]
    [InlineData("X-Request-Timestamp", "2026-06-12T10:00:00Z")]
    [InlineData("Accept", "*/*")]
    public void Non_secret_headers_pass_through(string header, string value)
    {
        Assert.False(HttpLogRedaction.IsSensitiveHeader(header));
        Assert.Equal(value, HttpLogRedaction.RedactHeaderValue(header, value));
    }

    [Fact]
    public void Bearer_token_value_never_appears_after_redaction()
    {
        var redacted = HttpLogRedaction.RedactHeaderValue("Authorization", "Bearer eyJhbGciOi.JWT.payload");
        Assert.DoesNotContain("eyJhbGciOi", redacted);
        Assert.Equal(HttpLogRedaction.Redacted, redacted);
    }

    [Theory]
    [InlineData("/api/patients")]
    [InlineData("/api/patients/123")]
    [InlineData("/api/patients/123/consultations")]
    [InlineData("/api/patients/123/invoices")]
    [InlineData("/api/consultations")]
    [InlineData("/api/invoices/42/pay")]
    [InlineData("/api/finance/summary")]
    [InlineData("/api/auth/login")]
    public void Sensitive_routes_suppress_bodies(string path)
    {
        Assert.True(HttpLogRedaction.IsSensitiveBodyPath(path));
    }

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/swagger/index.html")]
    [InlineData("/api/patientsearch")]   // not a /api/patients segment — must NOT match by prefix-string
    [InlineData(null)]
    [InlineData("")]
    public void Non_sensitive_routes_allow_bodies(string? path)
    {
        Assert.False(HttpLogRedaction.IsSensitiveBodyPath(path));
    }
}
