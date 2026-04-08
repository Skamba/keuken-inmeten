using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class KeukenStateServiceTests
{
    [Fact]
    public void Deur_toewijzing_bewaart_laatst_gebruikte_pot_hart_afstand()
    {
        var state = new KeukenStateService();

        state.VoegToewijzingToe(new PaneelToewijzing
        {
            Type = PaneelType.Deur,
            PotHartVanRand = 25
        });

        Assert.Equal(25, state.LaatstGebruiktePotHartVanRand);
    }

    [Fact]
    public void Laden_herstelt_laatst_gebruikte_pot_hart_afstand()
    {
        var state = new KeukenStateService();

        state.Laden(new KeukenData
        {
            LaatstGebruiktePotHartVanRand = 24.5
        });

        Assert.Equal(24.5, state.LaatstGebruiktePotHartVanRand);
    }

    [Fact]
    public void Exporteren_en_laden_bewaren_paneelrandspeling()
    {
        var state = new KeukenStateService();
        state.StelPaneelRandSpelingIn(1.5);

        var snapshot = state.Exporteren();
        var herladen = new KeukenStateService();
        herladen.Laden(snapshot);

        Assert.Equal(1.5, snapshot.PaneelRandSpeling);
        Assert.Equal(1.5, herladen.PaneelRandSpeling);
    }

    [Fact]
    public void WerkToewijzingBij_vervangt_bestaande_toewijzing_en_bewaart_laatst_gebruikte_pot_hart_afstand()
    {
        var state = new KeukenStateService();
        var toewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            Type = PaneelType.Deur,
            PotHartVanRand = 22.5,
            Breedte = 600,
            Hoogte = 2200
        };

        state.VoegToewijzingToe(toewijzing);

        state.WerkToewijzingBij(new PaneelToewijzing
        {
            Id = toewijzing.Id,
            Type = PaneelType.Deur,
            PotHartVanRand = 24.5,
            Breedte = 620,
            Hoogte = 2100
        });

        var bijgewerkt = Assert.Single(state.Toewijzingen);
        Assert.Equal(620, bijgewerkt.Breedte);
        Assert.Equal(2100, bijgewerkt.Hoogte);
        Assert.Equal(24.5, state.LaatstGebruiktePotHartVanRand);
    }

    [Fact]
    public void HernoemWand_triggert_exact_een_state_change()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var gewijzigd = state.HernoemWand(wand.Id, "Rechterwand");

        Assert.True(gewijzigd);
        Assert.Equal("Rechterwand", wand.Naam);
        Assert.Equal(1, notificaties);
    }

    [Fact]
    public void WerkWandAfmetingenBij_triggert_exact_een_state_change()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var gewijzigd = state.WerkWandAfmetingenBij(wand.Id, 2600, 2800, 120);

        Assert.True(gewijzigd);
        Assert.Equal(2600, wand.Breedte);
        Assert.Equal(2800, wand.Hoogte);
        Assert.Equal(120, wand.PlintHoogte);
        Assert.Equal(1, notificaties);
    }

    [Fact]
    public void Plank_commando_synchroniseren_state_change_pipeline()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);

        var kast = MaakKast("Onderkast");
        state.VoegKastToe(kast, wand.Id);

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var plank = state.VoegPlankToe(kast.Id, 320);
        Assert.NotNull(plank);
        Assert.Equal(1, notificaties);
        Assert.Single(kast.Planken);

        var verplaatst = state.VerplaatsPlank(kast.Id, plank!.Id, 352);
        Assert.True(verplaatst);
        Assert.Equal(352, kast.Planken[0].HoogteVanBodem);
        Assert.Equal(2, notificaties);

        var verwijderd = state.VerwijderPlank(kast.Id, plank.Id);
        Assert.True(verwijderd);
        Assert.Empty(kast.Planken);
        Assert.Equal(3, notificaties);

        var hersteld = state.HerstelPlank(kast.Id, plank, 0);
        Assert.NotNull(hersteld);
        Assert.Single(kast.Planken);
        Assert.Equal(plank.Id, kast.Planken[0].Id);
        Assert.Equal(4, notificaties);
    }

    private static KeukenWand MaakWand(string naam) => new()
    {
        Naam = naam,
        Breedte = 2400,
        Hoogte = 2700,
        PlintHoogte = 100
    };

    private static Kast MaakKast(string naam) => new()
    {
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
