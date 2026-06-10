using Medika.Domain.Common;

namespace Medika.Domain.Finance;

public interface IInvoiceRepository : IRepository<Invoice, InvoiceId>
{
    Task<IReadOnlyList<Invoice>> GetByPatientAsync(string cabinetId, string patientId, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetByStatusAsync(string cabinetId, InvoiceStatus status, string doctorId, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<decimal> SumByPeriodAsync(string cabinetId, string doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<string> GenerateNumberAsync(string cabinetId, CancellationToken ct = default);
}
