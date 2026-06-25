using FastEndpoints;

namespace Medika.Application.Identity.Commands.SetUserActive;

// UserId is bound from the {id} route segment; IsActive from the request body.
public record SetUserActiveCommand(string UserId, bool IsActive) : ICommand;
