using Medika.Domain.Common;

namespace Medika.Domain.Medical;

public sealed class PrescriptionLine : ValueObject
{
    public string Medication { get; }    // "AMOXICILLINE 500mg"
    public string Dosage { get; }        // "1 cp matin et soir"
    public string? Duration { get; }     // "7 jours"
    public int Quantity { get; }         // boxes

    public PrescriptionLine(string medication, string dosage, string? duration, int quantity)
    {
        if (string.IsNullOrWhiteSpace(medication)) throw new ArgumentException("Medication is required.");
        if (string.IsNullOrWhiteSpace(dosage)) throw new ArgumentException("Dosage is required.");
        Medication = medication;
        Dosage = dosage;
        Duration = duration;
        Quantity = quantity > 0 ? quantity : throw new ArgumentException("Quantity must be positive.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Medication;
        yield return Dosage;
        yield return Duration;
        yield return Quantity;
    }
}
