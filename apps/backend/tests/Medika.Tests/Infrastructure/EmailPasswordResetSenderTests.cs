using Medika.Application.Common.Interfaces;
using Medika.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Medika.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="EmailPasswordResetSender"/>. The email channel is captured
/// via a fake <see cref="IEmailService"/>, so no provider is contacted. Guards the
/// branded HTML template (issue #76) and the link/greeting it builds.
/// </summary>
public class EmailPasswordResetSenderTests
{
    private const string BaseUrl = "https://app.example.test";

    private static (EmailPasswordResetSender Sut, CapturingEmailService Email) CreateSut()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:BaseUrl"] = BaseUrl })
            .Build();
        var email = new CapturingEmailService();
        var sut = new EmailPasswordResetSender(config, email, NullLogger<EmailPasswordResetSender>.Instance);
        return (sut, email);
    }

    [Fact]
    public async Task Builds_reset_link_from_base_url_token_and_email()
    {
        var (sut, email) = CreateSut();

        await sut.SendResetLinkAsync("dr.kebir@example.test", "RAWTOKEN123", "Dr. Kader Kebir");

        var expectedLink = $"{BaseUrl}/reset-password?token=RAWTOKEN123&email=dr.kebir%40example.test";
        Assert.Contains(expectedLink, email.LastBody);
    }

    [Fact]
    public async Task Sends_html_to_the_requested_address_with_a_french_subject()
    {
        var (sut, email) = CreateSut();

        await sut.SendResetLinkAsync("dr.kebir@example.test", "tok", "Dr. Kader Kebir");

        Assert.Equal("dr.kebir@example.test", email.LastTo);
        Assert.True(email.LastIsHtml);
        Assert.Contains("mot de passe", email.LastSubject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Greets_the_user_by_name_when_provided()
    {
        var (sut, email) = CreateSut();

        await sut.SendResetLinkAsync("dr.kebir@example.test", "tok", "Dr. Kader Kebir");

        Assert.Contains("Bonjour", email.LastBody);
        Assert.Contains("Dr. Kader Kebir", email.LastBody);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Falls_back_to_a_generic_greeting_when_name_is_blank(string? name)
    {
        var (sut, email) = CreateSut();

        await sut.SendResetLinkAsync("user@example.test", "tok", name);

        Assert.Contains("Bonjour", email.LastBody);
        Assert.DoesNotContain("Dr.", email.LastBody);
    }

    [Fact]
    public async Task Renders_the_branded_template_blocks()
    {
        var (sut, email) = CreateSut();

        await sut.SendResetLinkAsync("user@example.test", "tok", "Dr. Kader Kebir");
        var body = email.LastBody!;

        // Hero + headline
        Assert.Contains("Réinitialisation", body);
        // CTA button label
        Assert.Contains("Réinitialiser mon mot de passe", body);
        // Expiry info box (link lifetime is 30 minutes)
        Assert.Contains("30 minutes", body);
        // Security box + contact (placeholder address per #76)
        Assert.Contains("securite@medika.dz", body);
        // Fallback "secours" link box
        Assert.Contains("LIEN DE SECOURS", body);
        // Footer tagline
        Assert.Contains("100% algérienne", body);
    }

    [Fact]
    public async Task References_hosted_png_assets_under_the_base_url()
    {
        var (sut, email) = CreateSut();

        await sut.SendResetLinkAsync("user@example.test", "tok", "Dr. Kader Kebir");
        var body = email.LastBody!;

        Assert.Contains($"{BaseUrl}/email/mark.png", body);
        Assert.Contains($"{BaseUrl}/email/lock-hero.png", body);
        Assert.Contains($"{BaseUrl}/email/clock.png", body);
        Assert.Contains($"{BaseUrl}/email/warning.png", body);
    }

    /// <summary>Captures the last email instead of sending it.</summary>
    private sealed class CapturingEmailService : IEmailService
    {
        public string? LastTo { get; private set; }
        public string? LastSubject { get; private set; }
        public string? LastBody { get; private set; }
        public bool LastIsHtml { get; private set; }

        public Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken ct = default)
        {
            LastTo = to;
            LastSubject = subject;
            LastBody = body;
            LastIsHtml = isHtml;
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken ct = default)
            => SendEmailAsync(to.First(), subject, body, isHtml, ct);
    }
}
