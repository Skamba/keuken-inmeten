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
            HeeftWandContext: false,
            HeeftSelectie: false,
            HeeftConceptPaneel: false,
            HeeftGeldigeMaat: false,
            RaaktGeselecteerdeKast: false,
            HeeftConflicterendPaneel: false,
            ActieveWandNaam: string.Empty,
            GeselecteerdeKastNamen: string.Empty);
        var klaar = new PaneelFlowContext(
            HeeftWandContext: true,
            HeeftSelectie: true,
            HeeftConceptPaneel: true,
            HeeftGeldigeMaat: true,
            RaaktGeselecteerdeKast: true,
            HeeftConflicterendPaneel: false,
            ActieveWandNaam: "Achterwand",
            GeselecteerdeKastNamen: "Kast 1");
        var conflict = new PaneelFlowContext(
            HeeftWandContext: true,
            HeeftSelectie: true,
            HeeftConceptPaneel: true,
            HeeftGeldigeMaat: true,
            RaaktGeselecteerdeKast: true,
            HeeftConflicterendPaneel: true,
            ActieveWandNaam: "Achterwand",
            GeselecteerdeKastNamen: "Kast 1");

        Assert.False(PaneelConfiguratieHelper.KanPaneelOpslaan(leeg));
        Assert.Equal("Nee, er is nog geen actieve wand.", PaneelConfiguratieHelper.BepaalOpslaanStatusTekst(leeg));
        Assert.Equal("active", PaneelConfiguratieHelper.BepaalPaneelFlowStatus("wand", leeg));

        Assert.True(PaneelConfiguratieHelper.KanPaneelOpslaan(klaar));
        Assert.Equal("Ja, u kunt nu opslaan.", PaneelConfiguratieHelper.BepaalOpslaanStatusTekst(klaar));
        Assert.Equal("active", PaneelConfiguratieHelper.BepaalPaneelFlowStatus("opslaan", klaar));

        Assert.False(PaneelConfiguratieHelper.KanPaneelOpslaan(conflict));
        Assert.Equal("Nee, het paneel overlapt nog een bestaand paneel.", PaneelConfiguratieHelper.BepaalOpslaanStatusTekst(conflict));
    }

    [Fact]
    public void BouwEditorStatus_geeft_wachtende_selectiestatus_voor_lege_editor()
    {
        var status = PaneelConfiguratieHelper.BouwEditorStatus(new PaneelEditorStatusContext(
            Flow: new PaneelFlowContext(
                HeeftWandContext: true,
                HeeftSelectie: false,
                HeeftConceptPaneel: false,
                HeeftGeldigeMaat: false,
                RaaktGeselecteerdeKast: false,
                HeeftConflicterendPaneel: false,
                ActieveWandNaam: "Achterwand",
                GeselecteerdeKastNamen: string.Empty),
            GeopendeWandNaam: "Achterwand",
            GeselecteerdeKastAantal: 0,
            ToonEditorDrawer: false,
            ToonCompacteEditorLeegstaat: true,
            IsBewerkModus: false,
            HeeftEnkeleKastSelectie: false,
            KanKastOpdelen: false,
            BewerkIndex: 0,
            OpdeelAnalyse: new PaneelOpdeelAnalyse(720, 0, 720, true, false)));

        Assert.Equal("Kastselectie — Achterwand", status.EditorDrawerTitel);
        Assert.Equal("Open editorlaag", status.OpenEditorKnopLabel);
        Assert.Equal("Selecteer eerst kast(en) in de tekening", status.WerklaagStatusTekst);
        Assert.Equal("Selecteer eerst kast(en) in de tekening.", status.WerkruimteStatusDetailTekst);
        Assert.Equal("Nog geen kast", status.SelectieSamenvatting);
        Assert.Equal("Nog 720 mm te verdelen.", status.OpdeelStatusTekst);
        Assert.Equal("alert-warning", status.OpdeelStatusClass);
    }

    [Fact]
    public void BouwEditorStatus_geeft_bewerkstatus_en_conflictteksten_terug()
    {
        var status = PaneelConfiguratieHelper.BouwEditorStatus(new PaneelEditorStatusContext(
            Flow: new PaneelFlowContext(
                HeeftWandContext: true,
                HeeftSelectie: true,
                HeeftConceptPaneel: true,
                HeeftGeldigeMaat: true,
                RaaktGeselecteerdeKast: true,
                HeeftConflicterendPaneel: true,
                ActieveWandNaam: "Achterwand",
                GeselecteerdeKastNamen: "Spoelkast"),
            GeopendeWandNaam: "Achterwand",
            GeselecteerdeKastAantal: 1,
            ToonEditorDrawer: true,
            ToonCompacteEditorLeegstaat: false,
            IsBewerkModus: true,
            HeeftEnkeleKastSelectie: true,
            KanKastOpdelen: true,
            BewerkIndex: 3,
            OpdeelAnalyse: new PaneelOpdeelAnalyse(720, 90, 630, false, false)));

        Assert.Equal("Paneel bewerken — Achterwand", status.EditorDrawerTitel);
        Assert.Equal("Paneel 3", status.EditorHeaderMeta);
        Assert.False(status.KanOpslaan);
        Assert.Equal("Verplaats of verklein het paneel totdat het geen bestaand paneel meer overlapt.", status.KernHintTekst);
        Assert.Equal("Nee, het paneel overlapt nog een bestaand paneel.", status.OpslaanStatusTekst);
        Assert.Equal("Spoelkast", status.SelectieSamenvatting);
        Assert.Equal("alert-danger", status.OpdeelStatusClass);
        Assert.Equal($"Elk deel moet minimaal {PaneelLayoutService.MinPaneelMaat:0.#} mm hoog zijn.", status.OpdeelStatusTekst);
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
    public void BepaalVrijeSegmenten_en_startrechthoek_behandelen_ook_deels_overlappende_panelen_als_bezet()
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
                Assert.Equal(300d, segment.HoogteVanVloer);
                Assert.Equal(100d, segment.Hoogte);
            },
            segment =>
            {
                Assert.Equal(700d, segment.HoogteVanVloer);
                Assert.Equal(300d, segment.Hoogte);
            });
        Assert.Equal(700d, start.HoogteVanVloer);
        Assert.Equal(300d, start.Hoogte);
    }

    [Fact]
    public void BepaalOpdeelBereik_geeft_gehele_selectie_terug_als_er_geen_bestaande_panelen_zijn()
    {
        var selectieBereik = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 0,
            Breedte = 600,
            Hoogte = 720
        };

        var bereik = PaneelConfiguratieHelper.BepaalOpdeelBereik(selectieBereik, []);

        Assert.NotNull(bereik);
        Assert.Equal(0d, bereik!.XPositie);
        Assert.Equal(0d, bereik.HoogteVanVloer);
        Assert.Equal(600d, bereik.Breedte);
        Assert.Equal(720d, bereik.Hoogte);
    }

    [Fact]
    public void BepaalOpdeelBereik_geeft_enkel_vrij_segment_terug_als_dat_de_enige_opening_is()
    {
        var selectieBereik = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 0,
            Breedte = 600,
            Hoogte = 720
        };
        var bestaandePanelen = new[]
        {
            new PaneelRechthoek { XPositie = 0, HoogteVanVloer = 0, Breedte = 600, Hoogte = 200 },
            new PaneelRechthoek { XPositie = 0, HoogteVanVloer = 500, Breedte = 600, Hoogte = 220 }
        };

        var bereik = PaneelConfiguratieHelper.BepaalOpdeelBereik(selectieBereik, bestaandePanelen);

        Assert.NotNull(bereik);
        Assert.Equal(200d, bereik!.HoogteVanVloer);
        Assert.Equal(300d, bereik.Hoogte);
    }

    [Fact]
    public void BepaalOpdeelBereik_geeft_null_als_meerdere_vrije_segmenten_overblijven()
    {
        var selectieBereik = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 0,
            Breedte = 600,
            Hoogte = 900
        };
        var bestaandePanelen = new[]
        {
            new PaneelRechthoek { XPositie = 0, HoogteVanVloer = 200, Breedte = 600, Hoogte = 100 },
            new PaneelRechthoek { XPositie = 0, HoogteVanVloer = 500, Breedte = 600, Hoogte = 100 }
        };

        var bereik = PaneelConfiguratieHelper.BepaalOpdeelBereik(selectieBereik, bestaandePanelen);

        Assert.Null(bereik);
    }

    [Fact]
    public void AnalyseerOpdeelHoogtes_en_bouw_segmenten_volgen_beschikbare_hoogte()
    {
        var analyse = PaneelConfiguratieHelper.AnalyseerOpdeelHoogtes(720, [200d, 220d, 300d]);
        var segmenten = PaneelConfiguratieHelper.BouwOpdeelSegmenten(
            new PaneelRechthoek
            {
                XPositie = 10,
                HoogteVanVloer = 100,
                Breedte = 600,
                Hoogte = 720
            },
            [200d, 220d, 300d]);

        Assert.True(analyse.KanBevestigen);
        Assert.Equal(0d, analyse.RestantHoogte);
        Assert.Collection(
            segmenten,
            segment =>
            {
                Assert.Equal(10d, segment.XPositie);
                Assert.Equal(100d, segment.HoogteVanVloer);
                Assert.Equal(600d, segment.Breedte);
                Assert.Equal(200d, segment.Hoogte);
            },
            segment =>
            {
                Assert.Equal(300d, segment.HoogteVanVloer);
                Assert.Equal(220d, segment.Hoogte);
            },
            segment =>
            {
                Assert.Equal(520d, segment.HoogteVanVloer);
                Assert.Equal(300d, segment.Hoogte);
            });
    }

    [Fact]
    public void MaakStandaardOpdeelHoogtes_deelt_exact_deelbare_hoogte_in_gelijke_stukken()
    {
        var hoogtes = PaneelConfiguratieHelper.MaakStandaardOpdeelHoogtes(720, 3);

        Assert.Equal([240d, 240d, 240d], hoogtes);
    }

    [Fact]
    public void MaakStandaardOpdeelHoogtes_verspreidt_rest_zodat_verschil_maximaal_1_mm_is()
    {
        // 704 / 3 = 234 rest 2 → two panels 235, one panel 234
        var hoogtes = PaneelConfiguratieHelper.MaakStandaardOpdeelHoogtes(704, 3);

        Assert.Equal(3, hoogtes.Count);
        Assert.Equal(704d, hoogtes.Sum());
        Assert.Equal(1d, hoogtes.Max() - hoogtes.Min());
    }

    [Fact]
    public void MaakStandaardOpdeelHoogtes_rest_1_geeft_zelfde_resultaat_als_vroeger()
    {
        // 700 / 3 = 233 rest 1: one panel 234 (at the end), two panels 233
        var hoogtes = PaneelConfiguratieHelper.MaakStandaardOpdeelHoogtes(700, 3);

        Assert.Equal([233d, 233d, 234d], hoogtes);
    }

    [Fact]
    public void MaakStandaardOpdeelHoogtes_grote_rest_spreidt_over_meerdere_panelen()
    {
        // 703 / 4 = 175 rest 3 → three panels 176, one panel 175
        var hoogtes = PaneelConfiguratieHelper.MaakStandaardOpdeelHoogtes(703, 4);

        Assert.Equal(4, hoogtes.Count);
        Assert.Equal(703d, hoogtes.Sum());
        Assert.Equal(1d, hoogtes.Max() - hoogtes.Min());
        Assert.Equal(3, hoogtes.Count(h => h == 176d));
    }

    [Fact]
    public void MaakStandaardOpdeelHoogtes_geeft_lege_lijst_bij_nul_of_negatief_aantal()
    {
        Assert.Empty(PaneelConfiguratieHelper.MaakStandaardOpdeelHoogtes(720, 0));
        Assert.Empty(PaneelConfiguratieHelper.MaakStandaardOpdeelHoogtes(720, -1));
    }

    [Fact]
    public void MaakStandaardOpdeelHoogtes_niet_geheel_totaal_valt_terug_op_oud_gedrag()
    {
        // 700.5 is not an integer → fallback: floor per panel except last
        var hoogtes = PaneelConfiguratieHelper.MaakStandaardOpdeelHoogtes(700.5, 3);

        Assert.Equal(3, hoogtes.Count);
        Assert.Equal(700.5, hoogtes.Sum(), precision: 1);
    }
}
