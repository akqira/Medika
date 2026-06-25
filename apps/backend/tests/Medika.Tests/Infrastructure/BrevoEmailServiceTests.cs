using System.Net;
using System.Text.Json;
using Medika.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Medika.Tests.Infrastructure;

/// <summary>
/// Unit tests for the Brevo transactional email provider. The HTTP layer is stubbed,
/// so no network call is made. Mirrors the eGestion Brevo tests, translated to xUnit.
/// </summary>
public class BrevoEmailServiceTests
{
    private readonly BrevoSettings _settings = new()
    {
        ApiKey = "test-api-key",
        FromEmail = "noreply@medika.test",
        FromName = "Medika",
        BaseUrl = "https://api.brevo.com/v3",
    };

    private (BrevoEmailService Sut, StubHttpMessageHandler Handler) CreateSut(BrevoSettings? settings = null)
    {
        var s = settings ?? _settings;
        var handler = new StubHttpMessageHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(s.BaseUrl.TrimEnd('/') + "/"),
        };
        httpClient.DefaultRequestHeaders.Add("api-key", s.ApiKey);
        var sut = new BrevoEmailService(httpClient, s, NullLogger<BrevoEmailService>.Instance);
        return (sut, handler);
    }

    [Fact]
    public async Task SendEmailAsync_posts_to_smtp_email_with_api_key()
    {
        var (sut, handler) = CreateSut();
        handler.Respond(HttpStatusCode.Created, "{\"messageId\":\"<abc@brevo>\"}");

        await sut.SendEmailAsync("user@example.com", "Subject", "<p>Hello</p>");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.LastRequest.RequestUri!.AbsoluteUri);
        Assert.Equal("test-api-key", Assert.Single(handler.LastRequest.Headers.GetValues("api-key")));
    }

    [Fact]
    public async Task SendEmailAsync_html_sets_html_content_sender_and_recipients()
    {
        var (sut, handler) = CreateSut();
        handler.Respond(HttpStatusCode.Created, "{}");

        await sut.SendEmailAsync("user@example.com", "Hi", "<p>Body</p>", isHtml: true);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        Assert.Equal("Hi", root.GetProperty("subject").GetString());
        Assert.Equal("<p>Body</p>", root.GetProperty("htmlContent").GetString());
        Assert.False(root.TryGetProperty("textContent", out _), "textContent must be omitted for HTML emails");

        Assert.Equal("noreply@medika.test", root.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("Medika", root.GetProperty("sender").GetProperty("name").GetString());

        var recipients = root.GetProperty("to").EnumerateArray().ToList();
        Assert.Equal("user@example.com", Assert.Single(recipients).GetProperty("email").GetString());
    }

    [Fact]
    public async Task SendEmailAsync_plain_text_sets_text_content_not_html()
    {
        var (sut, handler) = CreateSut();
        handler.Respond(HttpStatusCode.Created, "{}");

        await sut.SendEmailAsync("user@example.com", "Hi", "Plain body", isHtml: false);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        Assert.Equal("Plain body", root.GetProperty("textContent").GetString());
        Assert.False(root.TryGetProperty("htmlContent", out _), "htmlContent must be omitted for plain-text emails");
    }

    [Fact]
    public async Task SendEmailAsync_multiple_recipients_are_all_included()
    {
        var (sut, handler) = CreateSut();
        handler.Respond(HttpStatusCode.Created, "{}");

        await sut.SendEmailAsync(new[] { "a@x.com", "b@x.com" }, "Hi", "<p>x</p>");

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var emails = doc.RootElement.GetProperty("to").EnumerateArray()
            .Select(e => e.GetProperty("email").GetString())
            .ToList();
        Assert.Equal(new[] { "a@x.com", "b@x.com" }, emails);
    }

    [Fact]
    public async Task SendEmailAsync_non_success_status_throws()
    {
        var (sut, handler) = CreateSut();
        handler.Respond(HttpStatusCode.BadRequest, "{\"code\":\"invalid_parameter\",\"message\":\"bad\"}");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.SendEmailAsync("user@example.com", "Hi", "<p>x</p>"));
        Assert.Contains("400", ex.Message);
    }

    [Fact]
    public async Task SendEmailAsync_no_recipients_throws()
    {
        var (sut, _) = CreateSut();
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.SendEmailAsync(Array.Empty<string>(), "Hi", "<p>x</p>"));
    }

    [Fact]
    public async Task SendEmailAsync_when_not_configured_skips_send_without_throwing()
    {
        // Empty ApiKey + FromEmail = the local/CI default: send is skipped, no HTTP call.
        var unconfigured = new BrevoSettings { ApiKey = "", FromEmail = "", BaseUrl = "https://api.brevo.com/v3" };
        var (sut, handler) = CreateSut(unconfigured);

        await sut.SendEmailAsync("user@example.com", "Hi", "<p>x</p>");

        Assert.Null(handler.LastRequest); // never hit the wire
    }

    /// <summary>Captures the outgoing request and returns a canned response.</summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.Created;
        private string _responseBody = "{}";

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public void Respond(HttpStatusCode status, string body)
        {
            _status = status;
            _responseBody = body;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseBody),
            };
        }
    }
}
