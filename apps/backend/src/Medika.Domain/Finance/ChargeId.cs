namespace Medika.Domain.Finance;

public record ChargeId(Guid Value)
{
    public static ChargeId New() => new(Guid.NewGuid());
    public static ChargeId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
