using FastEndpoints;

namespace Medika.Application.Identity.Commands.SetUserPermissions;

// UserId is bound from the {id} route segment by the endpoint.
public record SetUserPermissionsCommand(string UserId, IReadOnlyList<string> Permissions) : ICommand;
