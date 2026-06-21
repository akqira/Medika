using Medika.Domain.Finance;
using Xunit;

namespace Medika.Tests.Finance;

public class ActTests
{
    [Fact]
    public void Create_sets_fields_and_is_active()
    {
        var act = Act.Create("cabinet-1", "  Consultation  ", 2000m);

        Assert.Equal("cabinet-1", act.CabinetId);
        Assert.Equal("Consultation", act.Name); // trimmed
        Assert.Equal(2000m, act.Tariff);
        Assert.True(act.IsActive);
        Assert.NotEqual(default, act.Id.Value);
    }

    [Fact]
    public void Create_allows_zero_tariff()
    {
        var act = Act.Create("cabinet-1", "Acte gratuit", 0m);
        Assert.Equal(0m, act.Tariff);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_name(string name)
        => Assert.Throws<ArgumentException>(() => Act.Create("cabinet-1", name, 1000m));

    [Fact]
    public void Create_rejects_negative_tariff()
        => Assert.Throws<ArgumentException>(() => Act.Create("cabinet-1", "Consultation", -1m));
}
