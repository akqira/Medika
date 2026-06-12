using FastEndpoints;

namespace Medika.Application.Finance.Queries.GetCharges;

public record GetChargesQuery(int Year, int Month) : ICommand<ChargesResult>;

public record ChargesResult(
    int Year,
    int Month,
    decimal TotalAmount,
    List<ChargeItem> Items);

public record ChargeItem(
    string ChargeId,
    string Category,
    string Description,
    decimal Amount,
    string Date,
    bool IsRecurring,
    DateTime CreatedAt);
