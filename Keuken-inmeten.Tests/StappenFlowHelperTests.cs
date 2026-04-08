using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class StappenFlowHelperTests
{
    [Fact]
    public void BepaalRouteGate_verwijst_naar_indeling_als_kasten_ontbreken()
    {
        var status = StappenFlowHelper.BepaalStatus(new KeukenStateService());

        var gate = StappenFlowHelper.BepaalRouteGate("panelen", status);

        Assert.NotNull(gate);
        Assert.Equal("kasten", gate!.VereisteStap.Id);
        Assert.Contains("ten minste één kast", gate.Reden);
    }

    [Fact]
    public void BepaalRouteGate_verwijst_naar_panelen_als_panelen_ontbreken()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Id = Guid.NewGuid(), Naam = "Achterwand", Breedte = 2400, Hoogte = 2700, PlintHoogte = 100 };
        state.VoegWandToe(wand);
        state.VoegKastToe(MaakKast("Kast 1"), wand.Id);

        var status = StappenFlowHelper.BepaalStatus(state);
        var gate = StappenFlowHelper.BepaalRouteGate("verificatie", status);

        Assert.NotNull(gate);
        Assert.Equal("panelen", gate!.VereisteStap.Id);
        Assert.Contains("Wijs eerst panelen toe", gate.Reden);
    }

    [Fact]
    public void BepaalVervolgStap_geeft_verificatie_als_kasten_en_panelen_bestaan()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Id = Guid.NewGuid(), Naam = "Achterwand", Breedte = 2400, Hoogte = 2700, PlintHoogte = 100 };
        state.VoegWandToe(wand);

        var kast = MaakKast("Kast 1");
        state.VoegKastToe(kast, wand.Id);
        state.VoegToewijzingToe(new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [kast.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Links,
            PotHartVanRand = 22.5,
            Breedte = 597,
            Hoogte = 717
        });

        var status = StappenFlowHelper.BepaalStatus(state);
        var vervolgStap = StappenFlowHelper.BepaalVervolgStap(status);

        Assert.Equal("verificatie", vervolgStap.Id);
    }

    private static Kast MaakKast(string naam) => new()
    {
        Id = Guid.NewGuid(),
        Naam = naam,
        Type = KastType.Onderkast,
        Breedte = 600,
        Hoogte = 720,
        Diepte = 560,
        Wanddikte = 18,
        GaatjesAfstand = 32,
        EersteGaatVanBoven = 19
    };
}
