using FastEndpoints;
using Medika.Application.Identity.Commands.SetUserActive;

namespace Medika.Api.Endpoints.Identity.Users;

public class SetUserActiveRequest
{
    public string Id { get; init; } = null!;   // bound from the {id} route segment
    public bool IsActive { get; init; }
}

public class SetUserActiveEndpoint : Endpoint<SetUserActiveRequest>
{
    public override void Configure()
    {
        Patch("/api/users/{id}/active");
        Permissions(PermissionConstants.Users.ManagePermissions);
        Summary(s => s.Summary = "Activate or deactivate a team member");
    }

    public override async Task HandleAsync(SetUserActiveRequest req, CancellationToken ct)
    {
        try
        {
            await new SetUserActiveCommand(req.Id, req.IsActive).ExecuteAsync(ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
        }
    }
}
