using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PaneelVerificatieStatusHelperTests
{
    [Fact]
    public void LeesStatus_geeft_standaardstatus_voor_onbekende_toewijzing()
    {
        var toewijzingId = Guid.NewGuid();

        var status = PaneelVerificatieStatusHelper.LeesStatus([], toewijzingId);

        Assert.Equal(toewijzingId, status.ToewijzingId);
        Assert.False(status.MatenOk);
        Assert.False(status.ScharnierPositiesOk);
    }

    [Fact]
    public void WerkStatusBij_voegt_nieuwe_status_toe_voor_bestaande_toewijzing()
    {
        var toewijzing = MaakToewijzing();
        var statussen = new List<PaneelVerificatieStatus>();

        var gewijzigd = PaneelVerificatieStatusHelper.WerkStatusBij(
            statussen,
            [toewijzing],
            toewijzing.Id,
            matenOk: true,
            scharnierPositiesOk: false);

        Assert.True(gewijzigd);
        var status = Assert.Single(statussen);
        Assert.Equal(toewijzing.Id, status.ToewijzingId);
        Assert.True(status.MatenOk);
        Assert.False(status.ScharnierPositiesOk);
    }

    [Fact]
    public void WerkStatusBij_doet_niets_bij_ongewijzigde_status()
    {
        var toewijzing = MaakToewijzing();
        List<PaneelVerificatieStatus> statussen =
        [
            new PaneelVerificatieStatus
            {
                ToewijzingId = toewijzing.Id,
                MatenOk = true,
                ScharnierPositiesOk = false
            }
        ];

        var gewijzigd = PaneelVerificatieStatusHelper.WerkStatusBij(
            statussen,
            [toewijzing],
            toewijzing.Id,
            matenOk: true,
            scharnierPositiesOk: false);

        Assert.False(gewijzigd);
        Assert.Single(statussen);
    }

    [Fact]
    public void VerwijderStatussenVoorToewijzingen_verwijdert_alleen_gekoppelde_statussen()
    {
        var behoudenId = Guid.NewGuid();
        var verwijderenId = Guid.NewGuid();
        List<PaneelVerificatieStatus> statussen =
        [
            new PaneelVerificatieStatus { ToewijzingId = behoudenId, MatenOk = true, ScharnierPositiesOk = true },
            new PaneelVerificatieStatus { ToewijzingId = verwijderenId, MatenOk = true, ScharnierPositiesOk = false }
        ];

        PaneelVerificatieStatusHelper.VerwijderStatussenVoorToewijzingen(statussen, [verwijderenId]);

        var status = Assert.Single(statussen);
        Assert.Equal(behoudenId, status.ToewijzingId);
    }

    private static PaneelToewijzing MaakToewijzing() => new()
    {
        Id = Guid.NewGuid(),
        Type = PaneelType.Deur,
        Breedte = 596,
        Hoogte = 716
    };
}
