using Medika.Domain.Common;

namespace Medika.Domain.Finance;

public interface IInvoiceRepository : IRepository<Invoice, InvoiceId>
{
    Task<IReadOnlyList<Invoice>> GetByPatientAsync(string patientId, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetByStatusAsync(InvoiceStatus status, string doctorId, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetByPeriodAsync(string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<decimal> SumByPeriodAsync(string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<string> GenerateNumberAsync(CancellationToken ct = default);
}
