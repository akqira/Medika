using FastEndpoints;

namespace Medika.Application.Finance.Commands.AddAct;

public record AddActCommand(string Name, decimal Tariff) : ICommand<string>;
