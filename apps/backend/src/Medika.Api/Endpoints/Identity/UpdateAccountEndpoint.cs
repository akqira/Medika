using FastEndpoints;
using Medika.Application.Identity.Commands.UpdateAccount;

namespace Medika.Api.Endpoints.Identity;

public class UpdateAccountRequest
{
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Email { get; init; } = null!;
}

public class UpdateAccountEndpoint : Endpoint<UpdateAccountRequest>
{
    public override void Configure()
    {
        Patch("/api/profile/account");
        Roles("Doctor");
    }

    public override async Task HandleAsync(UpdateAccountRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName))
            AddError(nameof(req.FirstName), "First name is required.");
        if (string.IsNullOrWhiteSpace(req.LastName))
            AddError(nameof(req.LastName), "Last name is required.");
        if (string.IsNullOrWhiteSpace(req.Email))
            AddError(nameof(req.Email), "Email is required.");

        if (ValidationFailed)
        {
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
            return;
        }

        try
        {
            var cmd = new UpdateAccountCommand(req.FirstName, req.LastName, req.Email);
            await cmd.ExecuteAsync(ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
        }
    }
}
