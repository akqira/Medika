using Medika.Domain.Finance;
using Xunit;

namespace Medika.Tests.Finance;

public class InvoiceTests
{
    [Fact]
    public void CreateFromConsultation_carries_trimmed_act_name()
    {
        var inv = Invoice.CreateFromConsultation("cab-1", "p1", "c1", "doc-1", 2000m, "F-2026-001", "  Consultation  ");
        Assert.Equal("Consultation", inv.ActName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromConsultation_normalises_blank_act_to_null(string? act)
    {
        var inv = Invoice.CreateFromConsultation("cab-1", "p1", "c1", "doc-1", 2000m, "F-2026-001", act);
        Assert.Null(inv.ActName);
    }
}
