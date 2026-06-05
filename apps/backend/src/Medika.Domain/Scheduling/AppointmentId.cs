namespace Medika.Domain.Scheduling;

public record AppointmentId(Guid Value)
{
    public static AppointmentId New() => new(Guid.NewGuid());
    public static AppointmentId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
