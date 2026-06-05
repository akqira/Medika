using Medika.Domain.Common;

namespace Medika.Domain.Finance.Events;

public record InvoicePaid(
    InvoiceId InvoiceId, string PatientId,
    decimal Amount, PaymentMethod Method) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
