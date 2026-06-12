using Medika.Domain.Common;

namespace Medika.Domain.Scheduling.Events;

public record AppointmentBooked(
    AppointmentId AppointmentId, string PatientId,
    DateOnly Date, TimeOnly Time) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
