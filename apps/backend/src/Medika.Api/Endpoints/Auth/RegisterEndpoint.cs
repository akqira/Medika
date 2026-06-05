using FastEndpoints;
using Medika.Application.Identity.Commands.Register;
using Medika.Domain.Identity;

namespace Medika.Api.Endpoints.Auth;

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

public record RegisterRequest(
    string Email, string Password,
    string FirstName, string LastName, string Role,
    string? Specialty = null, string? OrderNumber = null);

public record RegisterResponse(string UserId);
