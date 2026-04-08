using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class TerminologieHelperTests
{
    [Fact]
    public void HaalTermOp_geeft_taakgerichte_uitlegvelden()
    {
        var term = TerminologieHelper.HaalTermOp("cnc");

        Assert.Equal("Machinecoordinaten voor CNC", term.Label);
        Assert.Equal("CNC-coordinaten", term.TechnischeTerm);
        Assert.Contains("X- en Y-maten", term.Uitleg);
        Assert.Contains("productie", term.WanneerRelevant);
        Assert.Contains("X 37 mm", term.Voorbeeld);
        Assert.Contains("CNC-details", term.WatNuDoen);
    }

    [Fact]
    public void HaalTermOp_ondersteunt_eerste_gat_als_kernbegrip()
    {
        var term = TerminologieHelper.HaalTermOp("eerstegat");

        Assert.Equal("Start van de gaatjesrij", term.Label);
        Assert.Equal("eerste gat", term.TechnischeTerm);
        Assert.Contains("19 mm", term.Voorbeeld);
        Assert.Contains("standaard", term.WatNuDoen);
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
