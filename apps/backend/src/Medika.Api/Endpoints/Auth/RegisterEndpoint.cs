using FastEndpoints;
using FluentValidation;
using Medika.Application.Identity.Commands.Register;
using Medika.Domain.Identity;

namespace Medika.Api.Endpoints.Auth;

public class RegisterRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Role { get; init; } = null!;
    public string? Specialty { get; init; }
    public string? OrderNumber { get; init; }
}

public class RegisterValidator : Validator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => Enum.TryParse<Role>(r, ignoreCase: true, out _))
            .WithMessage(x => $"'{x.Role}' is not a valid role.");
    }
}

public class RegisterEndpoint : Endpoint<RegisterRequest, RegisterResponse>
{
    public override void Configure()
    {
        Post("/api/auth/register");
        Roles(nameof(Role.Doctor));
        Summary(s => s.Summary = "Register a new user (Doctor only)");
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var role = Enum.Parse<Role>(req.Role, ignoreCase: true);
        var id = await new RegisterCommand(
            req.Email, req.Password, req.FirstName, req.LastName,
            role, req.Specialty, req.OrderNumber).ExecuteAsync(ct);
        await HttpContext.Response.SendAsync(new RegisterResponse(id), 201, null, ct);
    }
}

public record RegisterResponse(string UserId);
