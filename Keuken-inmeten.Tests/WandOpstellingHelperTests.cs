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
    public void BepaalPlankHoogteVoorToevoegen_snapt_naar_dichtstbijzijnde_gatpositie()
    {
        var kast = new Kast
        {
            Hoogte = 720,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19,
            HoogteVanVloer = 0
        };

        var hoogte = WandOpstellingHelper.BepaalPlankHoogteVoorToevoegen(
            kast,
            svgY: 635,
            vloerY: 1000,
            schaal: 1);

        Assert.Equal(345d, hoogte);
    }

    [Fact]
    public void BepaalPlankHoogteNaDrop_snapt_naar_dichtstbijzijnde_gatpositie()
    {
        var kast = new Kast
        {
            Hoogte = 720,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19,
            HoogteVanVloer = 0
        };

        var hoogte = WandOpstellingHelper.BepaalPlankHoogteNaDrop(
            kast,
            svgCenterY: 635,
            vloerY: 1000,
            schaal: 1);

        Assert.Equal(345d, hoogte);
    }

    [Fact]
    public void BepaalPlankHoogteNaToets_gaat_naar_volgend_of_vorig_gat()
    {
        var kast = new Kast
        {
            Hoogte = 720,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19
        };
        var plank = new Plank { HoogteVanBodem = 345 };

        var omhoog = WandOpstellingHelper.BepaalPlankHoogteNaToets(kast, plank, "ArrowUp", stap: 1);
        var omlaag = WandOpstellingHelper.BepaalPlankHoogteNaToets(kast, plank, "ArrowDown", stap: 1);

        Assert.Equal(377d, omhoog);
        Assert.Equal(313d, omlaag);
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
            [],
            key: "ArrowRight",
            stap: 100,
            wandBreedte: 1900,
            wandHoogte: 2600,
            plintHoogte: 100);

        Assert.NotNull(positie);
        Assert.Equal(1300d, positie.Value.XPositie);
        Assert.Equal(1800d, positie.Value.HoogteVanVloer);
    }

    [Fact]
    public void BepaalKastPositieNaDrop_snapt_ook_binnen_25mm_naar_stapeling()
    {
        var kleineKast = new Kast { Breedte = 600, Hoogte = 315 };
        var hogeKast = new Kast { Breedte = 600, Hoogte = 1915, XPositie = 0, HoogteVanVloer = 0 };

        var positie = WandOpstellingHelper.BepaalKastPositieNaDrop(
            kleineKast,
            [hogeKast],
            svgX: 50,
            svgY: 396,
            padding: 50,
            schaal: 1,
            vloerY: 2650,
            wandBreedte: 2000,
            wandHoogte: 2600,
            plintHoogte: 100);

        Assert.Equal(0d, positie.XPositie);
        Assert.Equal(1915d, positie.HoogteVanVloer);
    }

    [Fact]
    public void BepaalKastPositieNaDrop_snapt_niet_buiten_25mm_naar_stapeling()
    {
        var kleineKast = new Kast { Breedte = 600, Hoogte = 315 };
        var hogeKast = new Kast { Breedte = 600, Hoogte = 1915, XPositie = 0, HoogteVanVloer = 0 };

        var positie = WandOpstellingHelper.BepaalKastPositieNaDrop(
            kleineKast,
            [hogeKast],
            svgX: 50,
            svgY: 380,
            padding: 50,
            schaal: 1,
            vloerY: 2650,
            wandBreedte: 2000,
            wandHoogte: 2600,
            plintHoogte: 100);

        Assert.Equal(0d, positie.XPositie);
        Assert.Equal(1960d, positie.HoogteVanVloer);
    }

    [Fact]
    public void BepaalKastPositieNaToets_snapt_naar_bijna_gelijkliggende_bovenkant()
    {
        var kleineKast = new Kast
        {
            Breedte = 600,
            Hoogte = 315,
            XPositie = 0,
            HoogteVanVloer = 1890
        };
        var hogeKast = new Kast
        {
            Breedte = 600,
            Hoogte = 1915,
            XPositie = 0,
            HoogteVanVloer = 0
        };

        var positie = WandOpstellingHelper.BepaalKastPositieNaToets(
            kleineKast,
            [hogeKast],
            key: "ArrowUp",
            stap: 1,
            wandBreedte: 2000,
            wandHoogte: 2600,
            plintHoogte: 100);

        Assert.NotNull(positie);
        Assert.Equal(1915d, positie.Value.HoogteVanVloer);
        Assert.Equal(0d, positie.Value.XPositie);
    }
}
