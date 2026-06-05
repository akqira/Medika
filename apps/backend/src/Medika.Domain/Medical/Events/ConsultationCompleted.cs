using Medika.Domain.Common;

namespace Medika.Domain.Medical.Events;

public record ConsultationCompleted(
    ConsultationId ConsultationId, string PatientId,
    string DoctorId, decimal Tariff, DateTime Date) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
