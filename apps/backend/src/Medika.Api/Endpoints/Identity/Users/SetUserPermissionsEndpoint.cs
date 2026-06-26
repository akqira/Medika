using FastEndpoints;
using Medika.Application.Identity.Commands.SetUserPermissions;

namespace Medika.Api.Endpoints.Identity.Users;

public class SetUserPermissionsRequest
{
    public string Id { get; init; } = null!;          // bound from the {id} route segment
    public List<string> Permissions { get; init; } = [];
}

public class SetUserPermissionsEndpoint : Endpoint<SetUserPermissionsRequest>
{
    public override void Configure()
    {
        Patch("/api/users/{id}/permissions");
        Permissions(PermissionConstants.Users.ManagePermissions);
        Summary(s => s.Summary = "Replace a team member's permission set");
    }

    public override async Task HandleAsync(SetUserPermissionsRequest req, CancellationToken ct)
    {
        try
        {
            await new SetUserPermissionsCommand(req.Id, req.Permissions).ExecuteAsync(ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, 400, null, ct);
        }
    }
}
