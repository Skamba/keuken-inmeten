using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PaneelSpelingServiceTests
{
    [Fact]
    public void Volledige_opening_tegen_kastranden_krijgt_speling_aan_alle_zijden()
    {
        var kast = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Hoge kast",
            Breedte = 600,
            Hoogte = 2200,
            XPositie = 0,
            HoogteVanVloer = 0
        };
        var opening = new PaneelRechthoek
        {
            XPositie = 0,
            HoogteVanVloer = 0,
            Breedte = 600,
            Hoogte = 2200
        };

        var maatInfo = PaneelSpelingService.BerekenMaatInfo(opening, [kast], [], 2);

        Assert.True(maatInfo.RaaktLinks);
        Assert.True(maatInfo.RaaktRechts);
        Assert.True(maatInfo.RaaktOnder);
        Assert.True(maatInfo.RaaktBoven);
        Assert.Equal(2, maatInfo.PaneelRechthoek.XPositie);
        Assert.Equal(2, maatInfo.PaneelRechthoek.HoogteVanVloer);
        Assert.Equal(596, maatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(2196, maatInfo.PaneelRechthoek.Hoogte);
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

        Assert.Equal(596, resultaat.Breedte);
        Assert.Equal(2196, resultaat.Hoogte);
        Assert.NotNull(resultaat.MaatInfo);
        Assert.Equal(600, resultaat.MaatInfo!.OpeningsRechthoek.Breedte);
        Assert.Equal(2200, resultaat.MaatInfo.OpeningsRechthoek.Hoogte);
    }
}
