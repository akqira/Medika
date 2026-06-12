using Medika.Domain.Common;

namespace Medika.Domain.Finance.Events;

public record ChargeAdded(
    ChargeId ChargeId, ChargeCategory Category,
    decimal Amount, DateOnly Date) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
