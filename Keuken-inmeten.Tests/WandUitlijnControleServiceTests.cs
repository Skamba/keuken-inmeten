using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class WandUitlijnControleServiceTests
{
    [Fact]
    public void BepaalWaarschuwingen_signaleert_bijna_flush_verticale_stapeling()
    {
        var wand = new KeukenWand { Id = Guid.NewGuid(), Naam = "Testwand" };
        var onder = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Onder",
            Breedte = 600,
            Hoogte = 1915,
            XPositie = 0,
            HoogteVanVloer = 100
        };
        var boven = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Boven",
            Breedte = 600,
            Hoogte = 315,
            XPositie = 0,
            HoogteVanVloer = 2020
        };
        wand.KastIds.AddRange([onder.Id, boven.Id]);

        var waarschuwingen = WandUitlijnControleService.BepaalWaarschuwingen([wand], [onder, boven]);

        var wandWaarschuwing = Assert.Single(waarschuwingen);
        var kastWaarschuwing = Assert.Single(wandWaarschuwing.Waarschuwingen);
        var afwijking = Assert.Single(kastWaarschuwing.Afwijkingen);
        Assert.Equal("verticale aansluiting", afwijking.Label);
        Assert.Equal(5d, afwijking.AfwijkingMm);
    }

    [Fact]
    public void BepaalWaarschuwingen_signaleert_bijna_gelijke_bovenkanten_tussen_kolommen()
    {
        var wand = new KeukenWand { Id = Guid.NewGuid(), Naam = "Testwand" };
        var links = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Links",
            Breedte = 600,
            Hoogte = 315,
            XPositie = 1800,
            HoogteVanVloer = 2015
        };
        var rechts = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Rechts",
            Breedte = 600,
            Hoogte = 315,
            XPositie = 2400,
            HoogteVanVloer = 2020
        };
        wand.KastIds.AddRange([links.Id, rechts.Id]);

        var waarschuwingen = WandUitlijnControleService.BepaalWaarschuwingen([wand], [links, rechts]);

        var wandWaarschuwing = Assert.Single(waarschuwingen);
        var kastWaarschuwing = Assert.Single(wandWaarschuwing.Waarschuwingen);
        Assert.Contains(kastWaarschuwing.Afwijkingen, item => item.Label == "onderkanten uit lijn" && item.AfwijkingMm == 5d);
        Assert.Contains(kastWaarschuwing.Afwijkingen, item => item.Label == "bovenkanten uit lijn" && item.AfwijkingMm == 5d);
    }

    [Fact]
    public void BepaalWaarschuwingen_slaat_exact_uitgelijnde_kasten_over()
    {
        var wand = new KeukenWand { Id = Guid.NewGuid(), Naam = "Testwand" };
        var onder = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Onder",
            Breedte = 600,
            Hoogte = 1915,
            XPositie = 0,
            HoogteVanVloer = 100
        };
        var boven = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Boven",
            Breedte = 600,
            Hoogte = 315,
            XPositie = 0,
            HoogteVanVloer = 2015
        };
        wand.KastIds.AddRange([onder.Id, boven.Id]);

        var waarschuwingen = WandUitlijnControleService.BepaalWaarschuwingen([wand], [onder, boven]);

        Assert.Empty(waarschuwingen);
    }
}
