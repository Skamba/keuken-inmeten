using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class ApparaatLayoutServiceTests
{
    [Fact]
    public void TryBepaalStandaardPlaatsing_kiest_de_plinthoogte_als_startpositie()
    {
        var wand = new KeukenWand
        {
            Id = Guid.NewGuid(),
            Naam = "Achterwand",
            Breedte = 1800,
            Hoogte = 1200,
            PlintHoogte = 100
        };
        var apparaat = MaakApparaat();

        var gevonden = ApparaatLayoutService.TryBepaalStandaardPlaatsing(
            wand,
            apparaat,
            [],
            [],
            out var plaatsing);

        Assert.True(gevonden);
        Assert.Equal(0, plaatsing.xPositie);
        Assert.Equal(100, plaatsing.hoogteVanVloer);
    }

    [Fact]
    public void TryBepaalStandaardPlaatsing_geeft_false_als_er_geen_vrije_plek_meer_is()
    {
        var wand = new KeukenWand
        {
            Id = Guid.NewGuid(),
            Naam = "Achterwand",
            Breedte = 1200,
            Hoogte = 600,
            PlintHoogte = 0
        };
        var apparaat = MaakApparaat();
        var bestaandeApparaten = new[]
        {
            MaakApparaat(xPositie: 0, hoogteVanVloer: 0),
            MaakApparaat(xPositie: 600, hoogteVanVloer: 0)
        };

        var gevonden = ApparaatLayoutService.TryBepaalStandaardPlaatsing(
            wand,
            apparaat,
            [],
            bestaandeApparaten,
            out _);

        Assert.False(gevonden);
    }

    private static Apparaat MaakApparaat(double xPositie = 0, double hoogteVanVloer = 0) => new()
    {
        Id = Guid.NewGuid(),
        Naam = "Oven",
        Type = ApparaatType.Oven,
        Breedte = 600,
        Hoogte = 600,
        Diepte = 560,
        XPositie = xPositie,
        HoogteVanVloer = hoogteVanVloer
    };
}
