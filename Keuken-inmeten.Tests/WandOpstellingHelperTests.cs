using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class WandOpstellingHelperTests
{
    [Fact]
    public void Markeringen_schakelen_om_naar_500mm_bij_grote_wanden()
    {
        Assert.Equal(new[] { 250, 500, 750 }, WandOpstellingHelper.BepaalBreedteMarkeringen(1000).ToArray());
        Assert.Equal(new[] { 500, 1000, 1500, 2000 }, WandOpstellingHelper.BepaalHoogteMarkeringen(2400).ToArray());
    }

    [Fact]
    public void BepaalKastPositieNaDrop_snapt_naar_bestaande_kast_en_plint()
    {
        var kast = new Kast { Breedte = 600, Hoogte = 720 };
        var buurKast = new Kast { Breedte = 600, Hoogte = 720, XPositie = 600, HoogteVanVloer = 100 };

        var positie = WandOpstellingHelper.BepaalKastPositieNaDrop(
            kast,
            [buurKast],
            svgX: 639,
            svgY: 1838,
            padding: 50,
            schaal: 1,
            vloerY: 2650,
            wandBreedte: 2000,
            wandHoogte: 2600,
            plintHoogte: 100);

        Assert.Equal(600d, positie.XPositie);
        Assert.Equal(100d, positie.HoogteVanVloer);
    }

    [Fact]
    public void BepaalPlankHoogteVoorToevoegen_rondt_af_op_gaatjesafstand()
    {
        var kast = new Kast
        {
            Hoogte = 720,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            HoogteVanVloer = 0
        };

        var hoogte = WandOpstellingHelper.BepaalPlankHoogteVoorToevoegen(
            kast,
            svgY: 635,
            vloerY: 1000,
            schaal: 1);

        Assert.Equal(352d, hoogte);
    }

    [Fact]
    public void BepaalKastPositieNaToets_clampt_binnen_wandgrenzen()
    {
        var kast = new Kast
        {
            Breedte = 600,
            Hoogte = 720,
            XPositie = 1250,
            HoogteVanVloer = 1800
        };

        var positie = WandOpstellingHelper.BepaalKastPositieNaToets(
            kast,
            key: "ArrowRight",
            stap: 100,
            wandBreedte: 1900,
            wandHoogte: 2600);

        Assert.NotNull(positie);
        Assert.Equal(1300d, positie.Value.XPositie);
        Assert.Equal(1800d, positie.Value.HoogteVanVloer);
    }
}
