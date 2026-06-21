using FastEndpoints;

namespace Medika.Application.Finance.Commands.DeleteCharge;

// Id is bound from the {id} route segment by the endpoint.
public record DeleteChargeCommand(string Id) : ICommand;
