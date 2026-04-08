using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class StapHulpHelperTests
{
    [Fact]
    public void Voor_iedere_wizardstap_bestaat_staphulp_met_alle_hulpniveaus()
    {
        foreach (var stap in StappenFlowHelper.AlleStappen)
        {
            var hulp = StapHulpHelper.VoorStap(stap.Id);

            Assert.Equal(stap.Id, hulp.StapId);
            Assert.Collection(
                hulp.Niveaus,
                niveau => Assert.Equal("hint", niveau.Id),
                niveau => Assert.Equal("info", niveau.Id),
                niveau => Assert.Equal("details", niveau.Id),
                niveau => Assert.Equal("drawer", niveau.Id));
            Assert.NotEmpty(hulp.Secties);
            Assert.All(hulp.Secties, sectie => Assert.NotEmpty(sectie.Punten));
        }
    }

    [Fact]
    public void VoorStap_geeft_duidelijke_fout_bij_onbekende_stap()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => StapHulpHelper.VoorStap("onbekend"));

        Assert.Contains("Onbekende staphulp", ex.Message);
    }
}
