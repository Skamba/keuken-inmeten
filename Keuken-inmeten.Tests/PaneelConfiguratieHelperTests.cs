using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PaneelConfiguratieHelperTests
{
    [Fact]
    public void Flow_status_en_opslaanstekst_volgen_context()
    {
        var leeg = new PaneelFlowContext(
            HeeftSelectie: false,
            HeeftConceptPaneel: false,
            HeeftGeldigeMaat: false,
            RaaktGeselecteerdeKast: false,
            ActieveWandNaam: string.Empty,
            GeselecteerdeKastNamen: string.Empty);
        var klaar = new PaneelFlowContext(
            HeeftSelectie: true,
            HeeftConceptPaneel: true,
            HeeftGeldigeMaat: true,
            RaaktGeselecteerdeKast: true,
            ActieveWandNaam: "Achterwand",
            GeselecteerdeKastNamen: "Kast 1");

        Assert.False(PaneelConfiguratieHelper.KanPaneelOpslaan(leeg));
        Assert.Equal("Nee, er is nog geen kastselectie.", PaneelConfiguratieHelper.BepaalOpslaanStatusTekst(leeg));
        Assert.Equal("active", PaneelConfiguratieHelper.BepaalPaneelFlowStatus("selecteren", leeg));

        Assert.True(PaneelConfiguratieHelper.KanPaneelOpslaan(klaar));
        Assert.Equal("Ja, u kunt nu opslaan.", PaneelConfiguratieHelper.BepaalOpslaanStatusTekst(klaar));
        Assert.Equal("active", PaneelConfiguratieHelper.BepaalPaneelFlowStatus("opslaan", klaar));
    }

    [Fact]
    public void SnapPaneel_move_snapt_naar_dichtstbijzijnde_targets_met_behoud_van_maat()
    {
        var selectieBereik = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 0,
            Breedte = 1000,
            Hoogte = 1000
        };
        var voorstel = new PaneelRechthoek
        {
            XPositie = 183,
            HoogteVanVloer = 117,
            Breedte = 200,
            Hoogte = 300
        };

        var resultaat = PaneelConfiguratieHelper.SnapPaneel(
            "move",
            voorstel,
            selectieBereik,
            xTargets: [0d, 200d, 400d],
            yTargets: [0d, 100d, 400d]);

        Assert.Equal(200d, resultaat.XPositie);
        Assert.Equal(100d, resultaat.HoogteVanVloer);
        Assert.Equal(200d, resultaat.Breedte);
        Assert.Equal(300d, resultaat.Hoogte);
    }

    [Fact]
    public void SnapPaneel_se_volgt_bestaande_rechterkant_en_bovenzijde_logica()
    {
        var selectieBereik = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 0,
            Breedte = 1000,
            Hoogte = 1000
        };
        var voorstel = new PaneelRechthoek
        {
            XPositie = 100,
            HoogteVanVloer = 87,
            Breedte = 295,
            Hoogte = 313
        };

        var resultaat = PaneelConfiguratieHelper.SnapPaneel(
            "se",
            voorstel,
            selectieBereik,
            xTargets: [0d, 400d],
            yTargets: [0d, 100d, 500d]);

        Assert.Equal(100d, resultaat.XPositie);
        Assert.Equal(90d, resultaat.HoogteVanVloer);
        Assert.Equal(300d, resultaat.Breedte);
        Assert.Equal(310d, resultaat.Hoogte);
    }

    [Fact]
    public void BepaalVrijeSegmenten_en_startrechthoek_gebruiken_alleen_gelijke_span()
    {
        var selectieBereik = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 0,
            Breedte = 600,
            Hoogte = 1000
        };
        var bestaandePanelen = new[]
        {
            new PaneelRechthoek { XPositie = 0, HoogteVanVloer = 0, Breedte = 600, Hoogte = 200 },
            new PaneelRechthoek { XPositie = 0, HoogteVanVloer = 400, Breedte = 600, Hoogte = 300 },
            new PaneelRechthoek { XPositie = 10, HoogteVanVloer = 200, Breedte = 580, Hoogte = 100 }
        };

        var vrijeSegmenten = PaneelConfiguratieHelper.BepaalVrijeSegmenten(selectieBereik, bestaandePanelen);
        var start = PaneelConfiguratieHelper.BepaalStartRechthoek(selectieBereik, vrijeSegmenten);

        Assert.Collection(
            vrijeSegmenten,
            segment =>
            {
                Assert.Equal(200d, segment.HoogteVanVloer);
                Assert.Equal(200d, segment.Hoogte);
            },
            segment =>
            {
                Assert.Equal(700d, segment.HoogteVanVloer);
                Assert.Equal(300d, segment.Hoogte);
            });
        Assert.Equal(700d, start.HoogteVanVloer);
        Assert.Equal(300d, start.Hoogte);
    }
}
