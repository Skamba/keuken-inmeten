using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class TerminologieHelperTests
{
    [Fact]
    public void HaalTermOp_geeft_plain_label_technische_term_en_uitleg()
    {
        var term = TerminologieHelper.HaalTermOp("cnc");

        Assert.Equal("Machinecoordinaten voor CNC", term.Label);
        Assert.Equal("CNC-coordinaten", term.TechnischeTerm);
        Assert.Contains("X- en Y-maten", term.Uitleg);
    }

    [Fact]
    public void HaalTermenOp_verwijdert_dubbelen_en_behoudt_volgorde()
    {
        var termen = TerminologieHelper.HaalTermenOp(["randspeling", "werkmaat", "Randspeling"]);

        Assert.Collection(
            termen,
            term => Assert.Equal("randspeling", term.Sleutel),
            term => Assert.Equal("werkmaat", term.Sleutel));
    }
}
