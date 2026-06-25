using FastEndpoints;
using FluentValidation;
using Medika.Application.Identity.Commands.CreateStaff;

namespace Medika.Api.Endpoints.Identity.Users;

public class CreateStaffRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    // Null → server applies the default secretary permission set.
    public List<string>? Permissions { get; init; }
}

public class CreateStaffValidator : Validator<CreateStaffRequest>
{
    public CreateStaffValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class CreateStaffEndpoint : Endpoint<CreateStaffRequest, CreateStaffResponse>
{
    public override void Configure()
    {
        Post("/api/users");
        Permissions(PermissionConstants.Users.Add);
        Summary(s => s.Summary = "Create a secretary in the current cabinet");
    }

    public override async Task HandleAsync(CreateStaffRequest req, CancellationToken ct)
    {
        try
        {
            var id = await new CreateStaffCommand(
                req.Email, req.Password, req.FirstName, req.LastName, req.Permissions).ExecuteAsync(ct);
            await HttpContext.Response.SendAsync(new CreateStaffResponse(id), 201, null, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
        }
    }
}

public record CreateStaffResponse(string UserId);
