using Medika.Domain.Common;
using Medika.Domain.Finance.Events;

namespace Medika.Domain.Finance;

public sealed class Charge : AggregateRoot<ChargeId>
{
    public string DoctorId { get; private set; } = null!;
    public ChargeCategory Category { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public bool IsRecurring { get; private set; }
    public DateTime CreatedAt { get; private init; }

    private Charge() { }

    public static Charge Add(
        string doctorId, ChargeCategory category,
        string description, decimal amount, DateOnly date,
        bool isRecurring = false)
    {
        var charge = new Charge
        {
            Id = ChargeId.New(),
            DoctorId = doctorId,
            Category = category,
            Description = description,
            Amount = amount > 0 ? amount : throw new ArgumentException("Amount must be positive."),
            Date = date,
            IsRecurring = isRecurring,
            CreatedAt = DateTime.UtcNow,
        };
        charge.Raise(new ChargeAdded(charge.Id, charge.Category, charge.Amount, charge.Date));
        return charge;
    }
}
