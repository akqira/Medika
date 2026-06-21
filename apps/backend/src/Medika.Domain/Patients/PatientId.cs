namespace Medika.Domain.Patients;

public record PatientId(Guid Value)
{
    public static PatientId New() => new(Guid.NewGuid());
    public static PatientId From(string value) => new(Guid.Parse(value));
    /// <summary>Parses an id, returning null for a malformed value (caller maps to 404, not 500).</summary>
    public static PatientId? TryFrom(string value) => Guid.TryParse(value, out var g) ? new PatientId(g) : null;
    public override string ToString() => Value.ToString();
}
