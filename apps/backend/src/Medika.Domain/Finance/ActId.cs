namespace Medika.Domain.Finance;

public record ActId(Guid Value)
{
    public static ActId New() => new(Guid.NewGuid());
    public static ActId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
