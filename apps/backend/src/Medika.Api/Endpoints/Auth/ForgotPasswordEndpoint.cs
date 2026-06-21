using FastEndpoints;
using FluentValidation;
using Medika.Application.Identity.Commands.ForgotPassword;
using Microsoft.Extensions.Hosting;

namespace Medika.Api.Endpoints.Auth;

public class ForgotPasswordValidator : Validator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ForgotPasswordEndpoint : Endpoint<ForgotPasswordCommand, ForgotPasswordResponse>
{
    public override void Configure()
    {
        Post("/api/auth/forgot-password");
        AllowAnonymous();
        // Throttle abuse/enumeration in real environments; skipped in Development
        // so the E2E suite isn't rate-limited. Keyed on the BFF-forwarded client IP.
        if (!Resolve<IHostEnvironment>().IsDevelopment())
            Throttle(hitLimit: 5, durationSeconds: 60, headerName: "X-Client-IP");
        Summary(s => s.Summary = "Request a password-reset link by email");
    }

    public override async Task<ForgotPasswordResponse> ExecuteAsync(ForgotPasswordCommand req, CancellationToken ct)
    {
        await req.ExecuteAsync(ct);
        // Always the same response — never reveal whether the email exists.
        return new ForgotPasswordResponse(
            "Si un compte existe pour cette adresse, un lien de réinitialisation a été envoyé.");
    }
}

public record ForgotPasswordResponse(string Message);
