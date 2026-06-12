using Medika.Domain.Common;

namespace Medika.Domain.Identity.Events;

public record UserCreated(UserId UserId, string Email, Role Role) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
