using FastEndpoints;

namespace Medika.Application.Finance.Commands.DeleteAct;

// Id is bound from the {id} route segment by the endpoint.
public record DeleteActCommand(string Id) : ICommand;
