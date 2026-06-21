using FastEndpoints;
using FluentValidation;
using Medika.Application.Identity.Commands.Login;
using Microsoft.Extensions.Hosting;

namespace Medika.Api.Endpoints.Auth;

public record LoginRequest(string Email, string Password);

public class LoginValidator : Validator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
        // Brute-force / enumeration protection in real environments. Skipped in
        // Development so the parallel E2E suite (many logins/min) isn't throttled.
        // Keyed on the BFF-forwarded client IP (X-Client-IP), not X-Forwarded-For
        // (which the host overwrites with the BFF's own IP).
        if (!Resolve<IHostEnvironment>().IsDevelopment())
            Throttle(hitLimit: 5, durationSeconds: 60, headerName: "X-Client-IP");
        Summary(s => s.Summary = "Authenticate and receive a JWT token");
    }

    public override async Task<LoginResponse> ExecuteAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await new LoginCommand(req.Email, req.Password).ExecuteAsync(ct);
        return new LoginResponse(result.Token, result.UserId, result.Role, result.FullName);
    }
}

public record LoginResponse(string Token, string UserId, string Role, string FullName);
