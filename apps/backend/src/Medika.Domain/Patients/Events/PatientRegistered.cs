using Medika.Domain.Common;

namespace Medika.Domain.Patients.Events;

public record PatientRegistered(PatientId PatientId, string FirstName, string LastName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
