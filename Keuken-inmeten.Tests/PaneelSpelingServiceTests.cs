using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PaneelSpelingServiceTests
{
    [Fact]
    public void Vrijstaand_paneel_krijgt_geen_speling_tegen_eigen_buitenzijden()
    {
        var opening = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 0,
            Breedte = 600,
            Hoogte = 2200
        };

        var maatInfo = PaneelSpelingService.BerekenMaatInfo(opening, [], 3);

        Assert.False(maatInfo.RaaktLinks);
        Assert.False(maatInfo.RaaktRechts);
        Assert.False(maatInfo.RaaktOnder);
        Assert.False(maatInfo.RaaktBoven);
        Assert.Equal(0, maatInfo.PaneelRechthoek.XPositie);
        Assert.Equal(0, maatInfo.PaneelRechthoek.HoogteVanVloer);
        Assert.Equal(600, maatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(2200, maatInfo.PaneelRechthoek.Hoogte);
    }

    [Fact]
    public void Paneel_krijgt_speling_tegen_andere_kast_en_apparaat()
    {
        var opening = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 100,
            Breedte = 600,
            Hoogte = 600
        };
        var buurKast = new PaneelRechthoek
        {
            XPositie = 600,
            HoogteVanVloer = 100,
            Breedte = 600,
            Hoogte = 600
        };
        var buurApparaat = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 700,
            Breedte = 600,
            Hoogte = 300
        };

        var maatInfo = PaneelSpelingService.BerekenMaatInfo(opening, [buurKast, buurApparaat], 3);

        Assert.False(maatInfo.RaaktLinks);
        Assert.True(maatInfo.RaaktRechts);
        Assert.False(maatInfo.RaaktOnder);
        Assert.True(maatInfo.RaaktBoven);
        Assert.Equal(597, maatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(597, maatInfo.PaneelRechthoek.Hoogte);
    }

    [Fact]
    public void Aangrenzende_panelen_verdelen_oneven_totale_voeg_over_beide_panelen()
    {
        var links = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 0,
            Breedte = 600,
            Hoogte = 2200
        };
        var rechts = new PaneelRechthoek
        {
            XPositie = 600,
            HoogteVanVloer = 0,
            Breedte = 600,
            Hoogte = 2200
        };

        var linksMaatInfo = PaneelSpelingService.BerekenMaatInfo(links, [], [rechts], 3);
        var rechtsMaatInfo = PaneelSpelingService.BerekenMaatInfo(rechts, [], [links], 3);

        Assert.Equal(1, linksMaatInfo.InkortingRechts);
        Assert.Equal(2, rechtsMaatInfo.InkortingLinks);
        Assert.Equal(599, linksMaatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(598, rechtsMaatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(3, rechtsMaatInfo.PaneelRechthoek.XPositie - linksMaatInfo.PaneelRechthoek.Rechterkant);
    }

    [Fact]
    public void State_resultaten_gebruiken_de_werkmaat_na_randspeling()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand
        {
            Id = Guid.NewGuid(),
            Naam = "Muur",
            Breedte = 600,
            Hoogte = 2700,
            PlintHoogte = 100
        };
        var kast = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Hoge kast",
            Type = KastType.HogeKast,
            Breedte = 600,
            Hoogte = 2200,
            Diepte = 560,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19,
            XPositie = 0,
            HoogteVanVloer = 100
        };

        state.VoegWandToe(wand);
        state.VoegKastToe(kast, wand.Id);
        state.VoegToewijzingToe(new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [kast.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Rechts,
            Breedte = 600,
            Hoogte = 2200
        });

        var resultaat = Assert.Single(state.BerekenResultaten());

        Assert.Equal(600, resultaat.Breedte);
        Assert.Equal(2200, resultaat.Hoogte);
        Assert.NotNull(resultaat.MaatInfo);
        Assert.Equal(600, resultaat.MaatInfo!.OpeningsRechthoek.Breedte);
        Assert.Equal(2200, resultaat.MaatInfo.OpeningsRechthoek.Hoogte);
    }
}
