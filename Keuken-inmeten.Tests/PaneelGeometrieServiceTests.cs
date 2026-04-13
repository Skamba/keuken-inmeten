using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PaneelGeometrieServiceTests
{
    [Fact]
    public void Voor_toewijzing_en_conceptpaneel_geeft_de_service_dezelfde_geometrie_en_werkmaat()
    {
        var linkerKast = NieuweKast("Links", 0, 100, 600, 2200);
        var rechterKast = NieuweKast("Rechts", 600, 100, 600, 2200);
        var bovenApparaat = new Apparaat
        {
            Id = Guid.NewGuid(),
            Naam = "Bovenkast",
            Type = ApparaatType.Koelkast,
            XPositie = 0,
            HoogteVanVloer = 2300,
            Breedte = 900,
            Hoogte = 250
        };
        var toewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [linkerKast.Id],
            Type = PaneelType.Deur,
            Breedte = 900,
            Hoogte = 2200,
            XPositie = 0,
            HoogteVanVloer = 100
        };
        var conceptPaneel = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 100,
            Breedte = 900,
            Hoogte = 2200
        };

        var voorToewijzing = PaneelGeometrieService.BerekenVoorToewijzing(
            toewijzing,
            [linkerKast],
            [linkerKast, rechterKast],
            [bovenApparaat],
            [],
            3);
        var voorConcept = PaneelGeometrieService.BerekenVoorConceptPaneel(
            conceptPaneel,
            [linkerKast, rechterKast],
            [bovenApparaat],
            [],
            3);

        Assert.NotNull(voorToewijzing);
        Assert.NotNull(voorConcept);

        AssertRechthoek(conceptPaneel, voorToewijzing!.OpeningsRechthoek);
        AssertRechthoek(voorToewijzing.OpeningsRechthoek, voorConcept!.OpeningsRechthoek);
        AssertRechthoek(voorToewijzing.WerkRechthoek, voorConcept.WerkRechthoek);
        Assert.Equal(new[] { linkerKast.Id, rechterKast.Id }, voorToewijzing.DragendeKasten.Select(kast => kast.Id).ToArray());
        Assert.Equal(
            voorToewijzing.DragendeKasten.Select(kast => kast.Id).ToArray(),
            voorConcept.DragendeKasten.Select(kast => kast.Id).ToArray());
        Assert.True(voorToewijzing.MaatInfo.RaaktBoven);
        Assert.True(voorConcept.MaatInfo.RaaktBoven);
        Assert.Equal(2197, voorToewijzing.WerkRechthoek.Hoogte);
    }

    [Fact]
    public void Buurdetectie_neemt_kasten_apparaten_en_andere_panelen_mee()
    {
        var dragendeKast = NieuweKast("Links", 0, 100, 600, 600);
        var buurKast = NieuweKast("Rechts", 600, 100, 600, 600);
        var buurApparaat = new Apparaat
        {
            Id = Guid.NewGuid(),
            Naam = "Apparaat boven",
            Type = ApparaatType.Oven,
            XPositie = 0,
            HoogteVanVloer = 700,
            Breedte = 600,
            Hoogte = 250
        };
        var buurPaneel = PaneelGeometrieService.MaakBronVoorToewijzing(
            new PaneelToewijzing
            {
                Id = Guid.NewGuid(),
                KastIds = [buurKast.Id],
                Type = PaneelType.BlindPaneel,
                Breedte = 600,
                Hoogte = 300,
                XPositie = 600,
                HoogteVanVloer = 100
            },
            [buurKast]);
        Assert.NotNull(buurPaneel);
        var toewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [dragendeKast.Id],
            Type = PaneelType.Deur,
            Breedte = 600,
            Hoogte = 600,
            XPositie = 0,
            HoogteVanVloer = 100
        };

        var resultaat = PaneelGeometrieService.BerekenVoorToewijzing(
            toewijzing,
            [dragendeKast],
            [dragendeKast, buurKast],
            [buurApparaat],
            [buurPaneel],
            3);

        Assert.NotNull(resultaat);
        Assert.Single(resultaat!.DragendeKasten);
        Assert.Equal(3, resultaat.BuurRechthoeken.Count);
        Assert.Contains(resultaat.BuurRechthoeken, rechthoek => HeeftMaten(rechthoek, 600, 100, 600, 600));
        Assert.Contains(resultaat.BuurRechthoeken, rechthoek => HeeftMaten(rechthoek, 0, 700, 600, 250));
        Assert.Contains(resultaat.BuurRechthoeken, rechthoek => HeeftMaten(rechthoek, 600, 100, 600, 300));
        Assert.False(resultaat.MaatInfo.RaaktLinks);
        Assert.True(resultaat.MaatInfo.RaaktRechts);
        Assert.False(resultaat.MaatInfo.RaaktOnder);
        Assert.True(resultaat.MaatInfo.RaaktBoven);
        Assert.Equal(599, resultaat.WerkRechthoek.Breedte);
        Assert.Equal(597, resultaat.WerkRechthoek.Hoogte);
    }

    [Fact]
    public void Buurkast_zonder_buurpaneel_geeft_geen_voeg()
    {
        var dragendeKast = NieuweKast("Links", 0, 100, 600, 600);
        var buurKast = NieuweKast("Rechts", 600, 100, 600, 600);
        var toewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [dragendeKast.Id],
            Type = PaneelType.Deur,
            Breedte = 600,
            Hoogte = 600,
            XPositie = 0,
            HoogteVanVloer = 100
        };

        var resultaat = PaneelGeometrieService.BerekenVoorToewijzing(
            toewijzing,
            [dragendeKast],
            [dragendeKast, buurKast],
            [],
            [],
            3);

        Assert.NotNull(resultaat);
        Assert.False(resultaat!.MaatInfo.RaaktRechts);
        Assert.Equal(600, resultaat.WerkRechthoek.Breedte);
    }

    [Fact]
    public void State_verdeelt_totale_voeg_tussen_aangrenzende_stapelpaneels_op_drie_mm()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand
        {
            Id = Guid.NewGuid(),
            Naam = "Muur",
            Breedte = 3200,
            Hoogte = 2700,
            PlintHoogte = 100
        };
        var linksOnder = NieuweKast("Links onder", 1800, 100, 600, 1920);
        var linksBoven = NieuweKast("Links boven", 1800, 2020, 600, 320);
        var rechtsOnder = NieuweKast("Rechts onder", 2400, 100, 600, 1920);
        var rechtsBoven = NieuweKast("Rechts boven", 2400, 2020, 600, 320);
        var linksToewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [linksBoven.Id, linksOnder.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Rechts,
            Breedte = 600,
            Hoogte = 2240
        };
        var rechtsToewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [rechtsBoven.Id, rechtsOnder.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Links,
            Breedte = 600,
            Hoogte = 2240
        };

        state.VoegWandToe(wand);
        state.VoegKastToe(linksOnder, wand.Id);
        state.VoegKastToe(linksBoven, wand.Id);
        state.VoegKastToe(rechtsOnder, wand.Id);
        state.VoegKastToe(rechtsBoven, wand.Id);
        state.VoegToewijzingToe(linksToewijzing);
        state.VoegToewijzingToe(rechtsToewijzing);

        var linksMaatInfo = state.BerekenPaneelMaatInfo(linksToewijzing);
        var rechtsMaatInfo = state.BerekenPaneelMaatInfo(rechtsToewijzing);

        Assert.NotNull(linksMaatInfo);
        Assert.NotNull(rechtsMaatInfo);
        Assert.Equal(1, linksMaatInfo!.InkortingRechts);
        Assert.Equal(2, rechtsMaatInfo!.InkortingLinks);
        Assert.Equal(599, linksMaatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(598, rechtsMaatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(3, rechtsMaatInfo.PaneelRechthoek.XPositie - linksMaatInfo.PaneelRechthoek.Rechterkant);
    }

    [Fact]
    public void State_berekenresultaten_gebruikt_naastliggende_panelen_op_dezelfde_wand()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand
        {
            Id = Guid.NewGuid(),
            Naam = "Muur",
            Breedte = 3200,
            Hoogte = 2700,
            PlintHoogte = 100
        };
        var linksOnder = NieuweKast("Links onder", 1800, 100, 600, 1920);
        var linksBoven = NieuweKast("Links boven", 1800, 2020, 600, 320);
        var rechtsOnder = NieuweKast("Rechts onder", 2400, 100, 600, 1920);
        var rechtsBoven = NieuweKast("Rechts boven", 2400, 2020, 600, 320);
        var linksToewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [linksBoven.Id, linksOnder.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Rechts,
            Breedte = 600,
            Hoogte = 2240
        };
        var rechtsToewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [rechtsBoven.Id, rechtsOnder.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Links,
            Breedte = 600,
            Hoogte = 2240
        };

        state.VoegWandToe(wand);
        state.VoegKastToe(linksOnder, wand.Id);
        state.VoegKastToe(linksBoven, wand.Id);
        state.VoegKastToe(rechtsOnder, wand.Id);
        state.VoegKastToe(rechtsBoven, wand.Id);
        state.VoegToewijzingToe(linksToewijzing);
        state.VoegToewijzingToe(rechtsToewijzing);

        var resultaten = state.BerekenResultaten().ToDictionary(item => item.ToewijzingId);

        var linksResultaat = Assert.Contains(linksToewijzing.Id, resultaten);
        var rechtsResultaat = Assert.Contains(rechtsToewijzing.Id, resultaten);
        Assert.NotNull(linksResultaat.MaatInfo);
        Assert.NotNull(rechtsResultaat.MaatInfo);
        Assert.Equal(1, linksResultaat.MaatInfo!.InkortingRechts);
        Assert.Equal(2, rechtsResultaat.MaatInfo!.InkortingLinks);
        Assert.Equal(599, linksResultaat.MaatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(598, rechtsResultaat.MaatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(3, rechtsResultaat.MaatInfo.PaneelRechthoek.XPositie - linksResultaat.MaatInfo.PaneelRechthoek.Rechterkant);
    }

    [Fact]
    public void State_berekent_dezelfde_paneelmaatinfo_als_de_centrale_geometrie_service()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand
        {
            Id = Guid.NewGuid(),
            Naam = "Muur",
            Breedte = 1200,
            Hoogte = 2700,
            PlintHoogte = 100
        };
        var linkerKast = NieuweKast("Links", 0, 100, 600, 2200);
        var rechterKast = NieuweKast("Rechts", 600, 100, 600, 2200);
        var bovenApparaat = new Apparaat
        {
            Id = Guid.NewGuid(),
            Naam = "Apparaat boven",
            Type = ApparaatType.Oven,
            XPositie = 0,
            HoogteVanVloer = 2300,
            Breedte = 900,
            Hoogte = 200
        };
        var toewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [linkerKast.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Rechts,
            Breedte = 900,
            Hoogte = 2200,
            XPositie = 0,
            HoogteVanVloer = 100
        };

        state.VoegWandToe(wand);
        state.VoegKastToe(linkerKast, wand.Id);
        state.VoegKastToe(rechterKast, wand.Id);
        state.VoegApparaatToe(bovenApparaat, wand.Id);
        state.VoegToewijzingToe(toewijzing);

        var maatInfo = state.BerekenPaneelMaatInfo(toewijzing);
        var verwachteGeometrie = PaneelGeometrieService.BerekenVoorConceptPaneel(
            new PaneelRechthoek
            {
                XPositie = 0,
                HoogteVanVloer = 100,
                Breedte = 900,
                Hoogte = 2200
            },
            state.KastenVoorWand(wand.Id),
            state.ApparatenVoorWand(wand.Id),
            state.Toewijzingen
                .Select(item => PaneelGeometrieService.MaakBronVoorToewijzing(item, state.ZoekKasten(item.KastIds)))
                .Where(bron => bron is not null)
                .Cast<PaneelGeometrieBron>()
                .ToList(),
            state.PaneelRandSpeling,
            toewijzing.Id);

        Assert.NotNull(maatInfo);
        Assert.NotNull(verwachteGeometrie);
        AssertRechthoek(verwachteGeometrie!.OpeningsRechthoek, maatInfo!.OpeningsRechthoek);
        AssertRechthoek(verwachteGeometrie.WerkRechthoek, maatInfo.PaneelRechthoek);
        Assert.Equal(verwachteGeometrie.MaatInfo.RaaktBoven, maatInfo.RaaktBoven);
        Assert.Equal(verwachteGeometrie.MaatInfo.RaaktRechts, maatInfo.RaaktRechts);
        Assert.Equal(verwachteGeometrie.MaatInfo.RaaktOnder, maatInfo.RaaktOnder);
        Assert.Equal(verwachteGeometrie.MaatInfo.RaaktLinks, maatInfo.RaaktLinks);
    }

    private static Kast NieuweKast(string naam, double xPositie, double hoogteVanVloer, double breedte, double hoogte)
        => new()
        {
            Id = Guid.NewGuid(),
            Naam = naam,
            Type = KastType.HogeKast,
            Breedte = breedte,
            Hoogte = hoogte,
            Diepte = 560,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19,
            XPositie = xPositie,
            HoogteVanVloer = hoogteVanVloer
        };

    private static void AssertRechthoek(PaneelRechthoek verwacht, PaneelRechthoek actual)
    {
        Assert.Equal(verwacht.XPositie, actual.XPositie);
        Assert.Equal(verwacht.HoogteVanVloer, actual.HoogteVanVloer);
        Assert.Equal(verwacht.Breedte, actual.Breedte);
        Assert.Equal(verwacht.Hoogte, actual.Hoogte);
    }

    private static bool HeeftMaten(PaneelRechthoek rechthoek, double xPositie, double hoogteVanVloer, double breedte, double hoogte)
        => rechthoek.XPositie == xPositie
            && rechthoek.HoogteVanVloer == hoogteVanVloer
            && rechthoek.Breedte == breedte
            && rechthoek.Hoogte == hoogte;
}
