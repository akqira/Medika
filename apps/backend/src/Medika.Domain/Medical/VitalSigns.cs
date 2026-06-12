using Medika.Domain.Common;

namespace Medika.Domain.Medical;

public sealed class VitalSigns : ValueObject
{
    public string? BloodPressure { get; }    // e.g. "120/80"
    public int? PulseRate { get; }           // bpm
    public decimal? Weight { get; }          // kg
    public decimal? Temperature { get; }     // °C
    public int? SpO2 { get; }               // %
    public decimal? Height { get; }          // cm

    public VitalSigns(
        string? bloodPressure = null,
        int? pulseRate = null,
        decimal? weight = null,
        decimal? temperature = null,
        int? spO2 = null,
        decimal? height = null)
    {
        BloodPressure = bloodPressure;
        PulseRate = pulseRate;
        Weight = weight;
        Temperature = temperature;
        SpO2 = spO2;
        Height = height;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BloodPressure;
        yield return PulseRate;
        yield return Weight;
        yield return Temperature;
        yield return SpO2;
        yield return Height;
    }
}
