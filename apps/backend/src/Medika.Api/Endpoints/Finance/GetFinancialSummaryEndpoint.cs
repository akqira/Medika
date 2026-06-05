using FastEndpoints;
using Medika.Application.Finance.Queries.GetFinancialSummary;

namespace Medika.Api.Endpoints.Finance;

public class GetFinancialSummaryEndpoint : Endpoint<FinancialSummaryRequest, FinancialSummary>
{
    public override void Configure()
    {
        Get("/api/finance/summary");
        Roles("Doctor");
    }

    public override async Task<FinancialSummary> ExecuteAsync(FinancialSummaryRequest req, CancellationToken ct)
    {
        return await new GetFinancialSummaryQuery(req.Year, req.Month).ExecuteAsync(ct);
    }
}

public record FinancialSummaryRequest(
    [property: QueryParam] int Year,
    [property: QueryParam] int Month);
