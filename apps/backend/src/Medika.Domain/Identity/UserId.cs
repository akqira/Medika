namespace Medika.Domain.Identity;

public record UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
