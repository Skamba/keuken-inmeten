using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class VerificatieChecklistHelperTests
{
    [Fact]
    public void BouwStatus_geeft_eerste_controle_terug_voor_ongecontroleerde_deur()
    {
        var resultaat = MaakResultaat(PaneelType.Deur, boorgatAantal: 2);

        var status = VerificatieChecklistHelper.BouwStatus(
            resultaat,
            new PaneelVerificatieStatus { ToewijzingId = resultaat.ToewijzingId });

        Assert.True(status.HeeftScharnierCheck);
        Assert.False(status.Geverifieerd);
        Assert.Equal(0, status.AantalAfgevinkt);
        Assert.Equal(2, status.TotaalChecks);
        Assert.Equal(2, status.OpenChecks);
        Assert.Equal("Meet de maat in de opening na", status.VolgendeControle);
        Assert.Equal("Vergelijk de echte opening met de maat hieronder voordat u verdergaat.", status.HuidigeControleHint);
        Assert.Equal("2 open", status.OpenChecksLabel);
        Assert.Equal("0/2 controles", status.PaneelTabStatusTekst);
    }

    [Fact]
    public void BouwStatus_verplaatst_deur_naar_scharniercontrole_na_matencheck()
    {
        var resultaat = MaakResultaat(PaneelType.Deur, boorgatAantal: 2);

        var status = VerificatieChecklistHelper.BouwStatus(
            resultaat,
            new PaneelVerificatieStatus
            {
                ToewijzingId = resultaat.ToewijzingId,
                MatenOk = true
            });

        Assert.False(status.Geverifieerd);
        Assert.Equal(1, status.AantalAfgevinkt);
        Assert.Equal(1, status.OpenChecks);
        Assert.Equal("Controleer de systeemgaten", status.VolgendeControle);
        Assert.Equal("Bevestig nu of de scharnierplaatposities in de gaatjesrij kloppen.", status.HuidigeControleHint);
        Assert.Equal("1 open", status.OpenChecksLabel);
        Assert.Equal("1/2 controles", status.PaneelTabStatusTekst);
    }

    [Fact]
    public void BouwStatus_markeert_niet_deur_zonder_boorgaten_als_klaar_na_matencheck()
    {
        var resultaat = MaakResultaat(PaneelType.BlindPaneel, boorgatAantal: 0);

        var status = VerificatieChecklistHelper.BouwStatus(
            resultaat,
            new PaneelVerificatieStatus
            {
                ToewijzingId = resultaat.ToewijzingId,
                MatenOk = true
            });

        Assert.False(status.HeeftScharnierCheck);
        Assert.True(status.Geverifieerd);
        Assert.Equal(1, status.AantalAfgevinkt);
        Assert.Equal(1, status.TotaalChecks);
        Assert.Equal(0, status.OpenChecks);
        Assert.Equal("Alle controles voor dit paneel zijn klaar", status.VolgendeControle);
        Assert.Equal("Alles klaar", status.OpenChecksLabel);
        Assert.Equal("Klaar", status.PaneelTabStatusTekst);
    }

    private static PaneelResultaat MaakResultaat(PaneelType type, int boorgatAantal)
        => new()
        {
            ToewijzingId = Guid.NewGuid(),
            Type = type,
            KastNaam = "Kast 1",
            KastIds = [Guid.NewGuid()],
            Boorgaten = Enumerable.Range(0, boorgatAantal)
                .Select(index => new Boorgat
                {
                    Diameter = 35,
                    X = 22.5,
                    Y = 100 + (index * 400)
                })
                .ToList()
        };
}
