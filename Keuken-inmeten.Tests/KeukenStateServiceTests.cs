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
}
