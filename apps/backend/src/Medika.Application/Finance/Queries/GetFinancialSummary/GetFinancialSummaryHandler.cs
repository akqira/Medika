using FastEndpoints;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;

namespace Medika.Application.Finance.Queries.GetFinancialSummary;

public class GetFinancialSummaryHandler(
    IInvoiceRepository invoices,
    IChargeRepository charges,
    ICurrentUserService currentUser) : ICommandHandler<GetFinancialSummaryQuery, FinancialSummary>
{
    public async Task<FinancialSummary> ExecuteAsync(GetFinancialSummaryQuery query, CancellationToken ct)
    {
        var monthStart = new DateOnly(query.Year, query.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var income = await invoices.SumByPeriodAsync(currentUser.UserId, monthStart, monthEnd, ct);
        var chargesTotal = await charges.SumByPeriodAsync(currentUser.UserId, monthStart, monthEnd, ct);
        var pendingInvoices = await invoices.GetByStatusAsync(InvoiceStatus.Pending, currentUser.UserId, ct);
        var monthInvoices = await invoices.GetByPeriodAsync(currentUser.UserId, monthStart, monthEnd, ct);

        // 6-month trend
        var trend = new List<MonthlyRevenue>();
        for (int i = 5; i >= 0; i--)
        {
            var m = monthStart.AddMonths(-i);
            var mEnd = m.AddMonths(1).AddDays(-1);
            var mIncome = await invoices.SumByPeriodAsync(currentUser.UserId, m, mEnd, ct);
            trend.Add(new MonthlyRevenue(m.ToString("MMM"), mIncome));
        }

        var breakdown = new List<RevenueBreakdown>
        {
            new("Consultations", income, 100)  // refined per act type in next iteration
        };

        return new FinancialSummary(
            income, chargesTotal, income - chargesTotal,
            monthInvoices.Count(x => x.Status == InvoiceStatus.Paid),
            pendingInvoices.Count,
            pendingInvoices.Sum(x => x.Amount),
            trend, breakdown);
    }
}
