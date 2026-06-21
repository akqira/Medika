using FastEndpoints;

namespace Medika.Application.Finance.Queries.GetActs;

public record GetActsQuery : ICommand<ActsResult>;

public record ActsResult(List<ActItem> Items);

public record ActItem(string ActId, string Name, decimal Tariff);
