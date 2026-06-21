using Medika.Domain.Common;

namespace Medika.Domain.Finance;

/// <summary>
/// A billable act/procedure in a cabinet's catalogue (e.g. "Consultation", "Certificat
/// médical") with a default tariff. Cabinet-scoped reference data shared by the cabinet's
/// doctors; selected on a consultation to pre-fill the honoraires.
/// </summary>
public sealed class Act : AggregateRoot<ActId>
{
    public string CabinetId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public decimal Tariff { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private init; }

    private Act() { }

    public static Act Create(string cabinetId, string name, decimal tariff)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Act name is required.");
        if (tariff < 0)
            throw new ArgumentException("Tariff must be zero or positive.");

        return new Act
        {
            Id = ActId.New(),
            CabinetId = cabinetId,
            Name = name.Trim(),
            Tariff = tariff,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
