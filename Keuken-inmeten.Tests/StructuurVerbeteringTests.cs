using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class VisualisatieHelperTests
{
    [Theory]
    [InlineData(KastType.Onderkast, "#f5deb3")]
    [InlineData(KastType.Bovenkast, "#d4e6f1")]
    [InlineData(KastType.HogeKast, "#d5f5e3")]
    public void KastKleur_geeft_juiste_kleur_per_type(KastType type, string verwachteKleur)
    {
        Assert.Equal(verwachteKleur, VisualisatieHelper.KastKleur(type));
    }

    [Theory]
    [InlineData(ApparaatType.Oven, "#e8d5c4")]
    [InlineData(ApparaatType.Koelkast, "#c4e8e8")]
    [InlineData(ApparaatType.Afzuigkap, "#d8d8d8")]
    public void ApparaatKleur_geeft_juiste_kleur_per_type(ApparaatType type, string verwachteKleur)
    {
        Assert.Equal(verwachteKleur, VisualisatieHelper.ApparaatKleur(type));
    }

    [Theory]
    [InlineData(PaneelType.Deur, "Deur")]
    [InlineData(PaneelType.LadeFront, "Ladefront")]
    [InlineData(PaneelType.BlindPaneel, "Blind paneel")]
    public void PaneelTypeLabel_geeft_juiste_label_per_type(PaneelType type, string verwachtLabel)
    {
        Assert.Equal(verwachtLabel, VisualisatieHelper.PaneelTypeLabel(type));
    }

    [Fact]
    public void Fmt_formatteert_met_invariant_culture_en_een_decimaal()
    {
        Assert.Equal("123.5", VisualisatieHelper.Fmt(123.456));
        Assert.Equal("0.0", VisualisatieHelper.Fmt(0));
    }

    [Fact]
    public void FmtData_formatteert_met_maximaal_drie_decimalen()
    {
        Assert.Equal("123.456", VisualisatieHelper.FmtData(123.456));
        Assert.Equal("100", VisualisatieHelper.FmtData(100.0));
        Assert.Equal("1.5", VisualisatieHelper.FmtData(1.5));
    }
}

public class KeukenStateServiceHelperTests
{
    [Fact]
    public void ZoekKasten_vindt_kasten_op_id_en_behoudt_volgorde()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Naam = "Muur", Breedte = 2400, Hoogte = 2700, PlintHoogte = 100 };
        state.VoegWandToe(wand);

        var kast1 = new Kast { Naam = "Kast A", Breedte = 600, Hoogte = 720, HoogteVanVloer = 100, XPositie = 0 };
        var kast2 = new Kast { Naam = "Kast B", Breedte = 400, Hoogte = 720, HoogteVanVloer = 100, XPositie = 600 };
        state.VoegKastToe(kast1, wand.Id);
        state.VoegKastToe(kast2, wand.Id);

        // Reversed order
        var gevonden = state.ZoekKasten([kast2.Id, kast1.Id]);

        Assert.Equal(2, gevonden.Count);
        Assert.Equal("Kast B", gevonden[0].Naam);
        Assert.Equal("Kast A", gevonden[1].Naam);
    }

    [Fact]
    public void ZoekKasten_negeert_ontbrekende_ids()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Naam = "Muur" };
        state.VoegWandToe(wand);

        var kast = new Kast { Naam = "Kast A" };
        state.VoegKastToe(kast, wand.Id);

        var gevonden = state.ZoekKasten([kast.Id, Guid.NewGuid()]);

        Assert.Single(gevonden);
        Assert.Equal("Kast A", gevonden[0].Naam);
    }

    [Fact]
    public void ApparatenVoorWand_vindt_apparaten_op_id_en_behoudt_volgorde()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Naam = "Muur", Breedte = 2400, Hoogte = 2700, PlintHoogte = 100 };
        state.VoegWandToe(wand);

        var apparaat1 = new Apparaat
        {
            Naam = "Oven",
            Type = ApparaatType.Oven,
            Breedte = 600,
            Hoogte = 600,
            Diepte = 560,
            XPositie = 0,
            HoogteVanVloer = 100
        };
        var apparaat2 = new Apparaat
        {
            Naam = "Magnetron",
            Type = ApparaatType.Magnetron,
            Breedte = 600,
            Hoogte = 450,
            Diepte = 560,
            XPositie = 700,
            HoogteVanVloer = 100
        };
        state.VoegApparaatToe(apparaat1, wand.Id);
        state.VoegApparaatToe(apparaat2, wand.Id);
        wand.ApparaatIds = [apparaat2.Id, apparaat1.Id];

        var gevonden = state.ApparatenVoorWand(wand.Id);

        Assert.Equal(2, gevonden.Count);
        Assert.Equal("Magnetron", gevonden[0].Naam);
        Assert.Equal("Oven", gevonden[1].Naam);
    }

    [Fact]
    public void ApparatenVoorWand_negeert_ontbrekende_ids()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Naam = "Muur", Breedte = 2400, Hoogte = 2700, PlintHoogte = 100 };
        state.VoegWandToe(wand);

        var apparaat = new Apparaat
        {
            Naam = "Oven",
            Type = ApparaatType.Oven,
            Breedte = 600,
            Hoogte = 600,
            Diepte = 560,
            XPositie = 0,
            HoogteVanVloer = 100
        };
        state.VoegApparaatToe(apparaat, wand.Id);
        wand.ApparaatIds = [apparaat.Id, Guid.NewGuid()];

        var gevonden = state.ApparatenVoorWand(wand.Id);

        var teruggevonden = Assert.Single(gevonden);
        Assert.Equal("Oven", teruggevonden.Naam);
    }

    [Fact]
    public void WandVoorKast_vindt_wand_die_kast_bevat()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Naam = "Linkerwand" };
        state.VoegWandToe(wand);

        var kast = new Kast { Naam = "Kast" };
        state.VoegKastToe(kast, wand.Id);

        var gevondenWand = state.WandVoorKast(kast.Id);

        Assert.NotNull(gevondenWand);
        Assert.Equal("Linkerwand", gevondenWand.Naam);
    }

    [Fact]
    public void WandVoorKast_geeft_null_voor_onbekend_id()
    {
        var state = new KeukenStateService();
        Assert.Null(state.WandVoorKast(Guid.NewGuid()));
    }

    [Fact]
    public void WandNaamVoorKasten_geeft_eerste_wandnaam()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Naam = "Achterwand" };
        state.VoegWandToe(wand);

        var kast = new Kast { Naam = "Kast" };
        state.VoegKastToe(kast, wand.Id);

        Assert.Equal("Achterwand", state.WandNaamVoorKasten([kast.Id]));
    }

    [Fact]
    public void WandNaamVoorKasten_geeft_standaardlabel_zonder_match()
    {
        var state = new KeukenStateService();
        Assert.Equal("—", state.WandNaamVoorKasten([Guid.NewGuid()]));
        Assert.Equal("Onbekend", state.WandNaamVoorKasten([Guid.NewGuid()], "Onbekend"));
    }
}

public class BestellijstServiceToewijzingBugTests
{
    /// <summary>
    /// Regression test: BestellijstService used positional index to look up toewijzing,
    /// which breaks when BerekenResultaten() skips toewijzingen without cabinets.
    /// The fix uses ToewijzingId-based lookup instead.
    /// </summary>
    [Fact]
    public void BerekenItems_koppelt_juiste_toewijzing_ook_bij_overgeslagen_toewijzingen()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand
        {
            Naam = "Muur",
            Breedte = 3000,
            Hoogte = 2600
        };
        state.VoegWandToe(wand);

        var kast = new Kast
        {
            Naam = "Onderkast",
            Type = KastType.Onderkast,
            Breedte = 600,
            Hoogte = 720,
            Diepte = 560,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19,
            HoogteVanVloer = 0,
            XPositie = 0
        };
        state.VoegKastToe(kast, wand.Id);

        // First toewijzing references a non-existent cabinet → will be skipped by BerekenResultaten
        var toewijzingZonderKast = new PaneelToewijzing
        {
            KastIds = [Guid.NewGuid()],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Links,
            Breedte = 400,
            Hoogte = 700
        };

        // Second toewijzing has the real cabinet
        var toewijzingMetKast = new PaneelToewijzing
        {
            KastIds = [kast.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Rechts,
            Breedte = 600,
            Hoogte = 720
        };

        state.VoegToewijzingToe(toewijzingZonderKast);
        state.VoegToewijzingToe(toewijzingMetKast);

        var items = BestellijstService.BerekenItems(state);

        // Only one result should come back (the one with the valid cabinet)
        var item = Assert.Single(items);
        Assert.Equal(600, item.Breedte);
        Assert.Equal(720, item.Hoogte);
        // The scharnier should match the second toewijzing (Rechts), not the first (Links)
        Assert.Equal("Rechts", item.ScharnierLabel);
    }
}
