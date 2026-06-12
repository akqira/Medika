namespace Medika.Domain.Patients;

public record PatientId(Guid Value)
{
    public static PatientId New() => new(Guid.NewGuid());
    public static PatientId From(string value) => new(Guid.Parse(value));
    /// <summary>Parses an id, returning null for a malformed