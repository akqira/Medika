using FastEndpoints;
using FluentValidation;
using Medika.Application.Identity.Commands.ResetPassword;
using Microsoft.Extensions.Hosting;

namespace Medika.Api.Endpoints.Auth;

public class ResetPasswordValidator : Validator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .WithMessage("Le mot de passe doit contenir au moins 8 caractères.");
    }
}

public class ResetPasswordEndpoint : Endpoint<ResetPasswordCommand, ResetPasswordResponse>
{
    public override void Configure()
    {
        Post("/api/auth/reset-password");
        AllowAnonymous();
        if (!Resolve<IHostEnvironment>().IsDevelopment())
            Throttle(hitLimit: 5, durationSeconds: 60);
        Summary(s => s.Summary = "Set a new password using a valid reset token");
    }

    public override async Task<ResetPasswordResponse> ExecuteAsync(ResetPasswordCommand req, CancellationToken ct)
    {
        // An invalid/expired token throws ArgumentException → 400 (GlobalExceptionHandler).
        await req.ExecuteAsync(ct);
        return new ResetPasswordResponse("Mot de passe réinitialisé. Vous pouvez vous connecter.");
    }
}

public record ResetPasswordResponse(string Message);
