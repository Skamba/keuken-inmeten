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
            2);
        var voorConcept = PaneelGeometrieService.BerekenVoorConceptPaneel(
            conceptPaneel,
            [linkerKast, rechterKast],
            [bovenApparaat],
            [],
            2);

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
        Assert.Equal(2198, voorToewijzing.WerkRechthoek.Hoogte);
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
            2);

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
        Assert.Equal(598, resultaat.WerkRechthoek.Breedte);
        Assert.Equal(598, resultaat.WerkRechthoek.Hoogte);
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
