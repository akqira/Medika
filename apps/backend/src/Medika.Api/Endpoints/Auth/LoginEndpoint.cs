using FastEndpoints;
using FluentValidation;
using Medika.Application.Identity.Commands.Login;

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
        Throttle(hitLimit: 5, durationSeconds: 60);
        Summary(s => s.Summary = "Authenticate and receive a JWT token");
    }

    public override async Task<LoginResponse> ExecuteAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await new LoginCommand(req.Email, req.Password).ExecuteAsync(ct);
        return new LoginResponse(result.Token, result.UserId, result.Role, result.FullName);
    }
}

public record LoginResponse(string Token, string UserId, string Role, string FullName);
