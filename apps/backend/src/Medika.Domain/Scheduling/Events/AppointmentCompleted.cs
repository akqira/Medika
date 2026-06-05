using Medika.Domain.Common;

namespace Medika.Domain.Scheduling.Events;

public record AppointmentCompleted(
    AppointmentId AppointmentId, string PatientId,
    string ConsultationId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
