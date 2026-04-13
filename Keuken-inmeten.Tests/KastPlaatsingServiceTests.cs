using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class KastPlaatsingServiceTests
{
    [Fact]
    public void TryVindVrijePlaatsing_gebruikt_huidige_positie_als_die_past()
    {
        var wand = MaakWand();
        var bestaandeKasten = new[]
        {
            MaakKast("Links", xPositie: 0)
        };
        var kast = MaakKast("Nieuw", xPositie: 600);

        var gevonden = KastPlaatsingService.TryVindVrijePlaatsing(wand, bestaandeKasten, kast, out var plaatsing);

        Assert.True(gevonden);
        Assert.Equal(600, plaatsing.XPositie);
        Assert.Equal(100, plaatsing.HoogteVanVloer);
    }

    [Fact]
    public void IsVrijePlaatsing_geeft_false_bij_overlap()
    {
        var wand = MaakWand();
        var bestaandeKasten = new[]
        {
            MaakKast("Links", xPositie: 0)
        };
        var kast = MaakKast("Nieuw", xPositie: 0);

        var vrij = KastPlaatsingService.IsVrijePlaatsing(wand, bestaandeKasten, kast, xPositie: 0, hoogteVanVloer: 100);

        Assert.False(vrij);
    }

    [Fact]
    public void TryVindVrijePlaatsing_vindt_een_vrije_verticale_plek_boven_bestaande_kasten()
    {
        var wand = MaakWand(breedte: 1200, hoogte: 2000);
        var bestaandeKasten = new[]
        {
            MaakKast("Links", xPositie: 0),
            MaakKast("Rechts", xPositie: 600)
        };
        var kast = MaakKast("Boven", xPositie: 0);

        var gevonden = KastPlaatsingService.TryVindVrijePlaatsing(wand, bestaandeKasten, kast, out var plaatsing);

        Assert.True(gevonden);
        Assert.Equal(0, plaatsing.XPositie);
        Assert.Equal(820, plaatsing.HoogteVanVloer);
    }

    private static KeukenWand MaakWand(double breedte = 2400, double hoogte = 2700) => new()
    {
        Id = Guid.NewGuid(),
        Naam = "Wand",
        Breedte = breedte,
        Hoogte = hoogte,
        PlintHoogte = 100
    };

    private static Kast MaakKast(string naam, double xPositie) => new()
    {
        Id = Guid.NewGuid(),
        Naam = naam,
        Type = KastType.Onderkast,
        Breedte = 600,
        Hoogte = 720,
        Diepte = 560,
        Wanddikte = 18,
        GaatjesAfstand = 32,
        EersteGaatVanBoven = 19,
        HoogteVanVloer = 100,
        XPositie = xPositie
    };
}
