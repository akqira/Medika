using Medika.Domain.Common;

namespace Medika.Domain.Patients;

public sealed class BloodGroup : ValueObject
{
    public static readonly string[] ValidGroups = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];

    public string Value { get; }

    private BloodGroup(string value) => Value = value;

    public static BloodGroup From(string value)
    {
        if (!ValidGroups.Contains(value.ToUpperInvariant()))
            throw new ArgumentException($"Invalid blood group: {value}");
        return new BloodGroup(value.ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
