using Medika.Application.Finance.Queries.GetFinancialSummary;
using Medika.Domain.Finance;
using Medika.Tests.Fakes;
using Xunit;

namespace Medika.Tests.Finance;

public class GetFinancialSummaryHandlerTests
{
    private static Invoice Paid(string? act, decimal amount)
    {
        var inv = Invoice.CreateFromConsultation("cab-1", "p", "c", "user-1", amount, "F-2026-001", act);
        inv.MarkPaid(PaymentMethod.Cash);
        return inv;
    }

    [Fact]
    public async Task Breakdown_groups_paid_invoices_by_act_largest_first()
    {
        var monthInvoices = new List<Invoice>
        {
            Paid("Consultation", 2000m),
            Paid("Consultation", 2000m),
            Paid("Certificat", 1000m),
            Paid(null, 500m), // → "Autres"
        };
        var handler = new GetFinancialSummaryHandler(
            new FakeInvoiceRepository(monthInvoices, income: 5500m),
            new FakeChargeRepository(chargesTotal: 1500m),
            new FakeCurrentUserService());

        var result = await handler.ExecuteAsync(new GetFinancialSummaryQuery(2026, 6), CancellationToken.None);

        Assert.Equal(5500m, result.TotalIncome);
        Assert.Equal(1500m, result.TotalCharges);
        Assert.Equal(4000m, result.NetIncome);

        var byLabel = result.BreakdownByType.ToDictionary(b => b.Label, b => b.Amount);
        Assert.Equal(4000m, byLabel["Consultation"]);
        Assert.Equal(1000m, byLabel["Certificat"]);
        Assert.Equal(500m, byLabel["Autres"]);
        Assert.Equal("Consultation", result.BreakdownByType[0].Label); // largest first
        Assert.Equal(73, result.BreakdownByType[0].Percentage);        // 4000/5500 ≈ 73%
    }

    // ── Minimal fakes (only the methods the handler calls are real) ──

    private sealed class FakeInvoiceRepository(IReadOnlyList<Invoice> month, decimal income) : IInvoiceRepository
    {
        public Task<decimal> SumByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => Task.FromResult(income);
        public Task<IReadOnlyList<Invoice>> GetByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => Task.FromResult(month);
        public Task<IReadOnlyList<Invoice>> GetByStatusAsync(string cabinetId, InvoiceStatus status, string doctorId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Invoice>>([]);

        public Task<IReadOnlyList<Invoice>> GetByPatientAsync(string cabinetId, string patientId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<string> GenerateNumberAsync(string cabinetId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Invoice?> GetByIdAsync(InvoiceId id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(Invoice aggregate, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(Invoice aggregate, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(InvoiceId id, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeChargeRepository(decimal chargesTotal) : IChargeRepository
    {
        public Task<decimal> SumByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => Task.FromResult(chargesTotal);

        public Task<IReadOnlyList<Charge>> GetByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Charge>> GetByDoctorAndMonthAsync(string cabinetId, string doctorId, int year, int month, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Charge?> GetByIdAsync(ChargeId id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(Charge aggregate, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(Charge aggregate, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(ChargeId id, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
