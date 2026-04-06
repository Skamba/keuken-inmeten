using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PaneelLayoutServiceTests
{
    [Fact]
    public void BepaalVrijeVerticaleSegmenten_geeft_gat_tussen_bestaande_panelen_terug()
    {
        var gebied = new PaneelRechthoek
        {
            XPositie = 600,
            Breedte = 1200,
            HoogteVanVloer = 100,
            Hoogte = 785
        };
        var bezet = new[]
        {
            new PaneelRechthoek
            {
                XPositie = 600,
                Breedte = 1200,
                HoogteVanVloer = 100,
                Hoogte = 140
            },
            new PaneelRechthoek
            {
                XPositie = 600,
                Breedte = 1200,
                HoogteVanVloer = 560,
                Hoogte = 325
            }
        };

        var vrijeSegmenten = PaneelLayoutService.BepaalVrijeVerticaleSegmenten(gebied, bezet);

        var segment = Assert.Single(vrijeSegmenten);
        Assert.Equal(600, segment.XPositie);
        Assert.Equal(1200, segment.Breedte);
        Assert.Equal(240, segment.HoogteVanVloer);
        Assert.Equal(320, segment.Hoogte);
    }

    [Fact]
    public void BepaalVrijeVerticaleSegmenten_voegt_overlappende_bezette_stukken_samen()
    {
        var gebied = new PaneelRechthoek
        {
            XPositie = 0,
            Breedte = 600,
            HoogteVanVloer = 100,
            Hoogte = 700
        };
        var bezet = new[]
        {
            new PaneelRechthoek
            {
                XPositie = 0,
                Breedte = 600,
                HoogteVanVloer = 200,
                Hoogte = 160
            },
            new PaneelRechthoek
            {
                XPositie = 0,
                Breedte = 600,
                HoogteVanVloer = 300,
                Hoogte = 180
            }
        };

        var vrijeSegmenten = PaneelLayoutService.BepaalVrijeVerticaleSegmenten(gebied, bezet);

        Assert.Equal(2, vrijeSegmenten.Count);
        Assert.Equal(100, vrijeSegmenten[0].HoogteVanVloer);
        Assert.Equal(100, vrijeSegmenten[0].Hoogte);
        Assert.Equal(480, vrijeSegmenten[1].HoogteVanVloer);
        Assert.Equal(320, vrijeSegmenten[1].Hoogte);
    }
}
