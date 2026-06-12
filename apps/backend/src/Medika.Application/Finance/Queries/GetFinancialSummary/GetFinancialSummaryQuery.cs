using FastEndpoints;

namespace Medika.Application.Finance.Queries.GetFinancialSummary;

public record GetFinancialSummaryQuery(int Year, int Month) : ICommand<FinancialSummary>;

public record FinancialSummary(
    decimal TotalIncome,
    decimal TotalCharges,
    decimal NetIncome,
    int PaidInvoices,
    int PendingInvoices,
    decimal PendingAmount,
    List<MonthlyRevenue> MonthlyTrend,
    List<RevenueBreakdown> BreakdownByType);

public record MonthlyRevenue(string Month, decimal Amount);
public record RevenueBreakdown(string Label, decimal Amount, int Percentage);
