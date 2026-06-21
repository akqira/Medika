using Medika.Domain.Common;
using Medika.Domain.Finance.Events;

namespace Medika.Domain.Finance;

public sealed class Invoice : AggregateRoot<InvoiceId>
{
    public string CabinetId { get; private set; } = null!;
    public string PatientId { get; private set; } = null!;
    public string ConsultationId { get; private set; } = null!;
    public string DoctorId { get; private set; } = null!;
    public string Number { get; private init; } = null!;          // F-2026-001
    public decimal Amount { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime IssuedAt { get; private init; }
    // The catalogue act billed (for revenue breakdown). Null = free/unspecified act.
    public string? ActName { get; private set; }

    private Invoice() { }

    public static Invoice CreateFromConsultation(
        string cabinetId,
        string patientId, string consultationId,
        string doctorId, decimal amount, string number,
        string? actName = null)
    {
        return new Invoice
        {
            Id = InvoiceId.New(),
            CabinetId = cabinetId,
            PatientId = patientId,
            ConsultationId = consultationId,
            DoctorId = doctorId,
            Amount = amount,
            Number = number,
            Status = InvoiceStatus.Pending,
            IssuedAt = DateTime.UtcNow,
            ActName = string.IsNullOrWhiteSpace(actName) ? null : actName.Trim(),
        };
    }

    public void MarkPaid(PaymentMethod method)
    {
        if (Status != InvoiceStatus.Pending)
            throw new InvalidOperationException($"Cannot pay invoice in status {Status}.");
        Status = InvoiceStatus.Paid;
        PaymentMethod = method;
        PaidAt = DateTime.UtcNow;
        Raise(new InvoicePaid(Id, PatientId, Amount, method));
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid invoice.");
        Status = InvoiceStatus.Cancelled;
    }
}
