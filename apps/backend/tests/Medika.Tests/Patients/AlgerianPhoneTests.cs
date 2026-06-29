using Medika.Application.Common.Validation;
using Xunit;

namespace Medika.Tests.Patients;

/// <summary>
/// The shared phone rule (issue #124) must accept mobiles of every operator AND
/// landlines (which the old mobile-only regex wrongly rejected), and reject junk.
/// </summary>
public class AlgerianPhoneTests
{
    [Theory]
    [InlineData("0555123456")]   // mobile — Ooredoo
    [InlineData("0661234567")]   // mobile — Mobilis
    [InlineData("0791234567")]   // mobile — Djezzy
    [InlineData("021234567")]    // fixe — Alger (9 digits)
    [InlineData("031924567")]    // fixe — Constantine
    [InlineData("0555 12 34 56")] // spaces ignored
    [InlineData("021 23 45 67")]  // landline with spaces
    public void Accepts_valid_mobiles_and_landlines(string value)
        => Assert.True(AlgerianPhone.IsValid(value));

    [Theory]
    [InlineData("12345")]         // no leading 0, too short
    [InlineData("0812345678")]    // 08 is neither mobile nor fixe
    [InlineData("05551234")]      // mobile too short
    [InlineData("02123456")]      // landline too short (8)
    [InlineData("0212345678")]    // landline too long (10)
    [InlineData("055512345a")]    // contains a letter
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_malformed_numbers(string? value)
        => Assert.False(AlgerianPhone.IsValid(value));
}
