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

        var maatInfo = PaneelSpelingService.BerekenMaatInfo(opening, [], 2);

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

        var maatInfo = PaneelSpelingService.BerekenMaatInfo(opening, [buurKast, buurApparaat], 2);

        Assert.False(maatInfo.RaaktLinks);
        Assert.True(maatInfo.RaaktRechts);
        Assert.False(maatInfo.RaaktOnder);
        Assert.True(maatInfo.RaaktBoven);
        Assert.Equal(598, maatInfo.PaneelRechthoek.Breedte);
        Assert.Equal(598, maatInfo.PaneelRechthoek.Hoogte);
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
