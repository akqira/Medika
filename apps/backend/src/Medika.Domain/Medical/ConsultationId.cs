namespace Medika.Domain.Medical;

public record ConsultationId(Guid Value)
{
    public static ConsultationId New() => new(Guid.NewGuid());
    public static ConsultationId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
