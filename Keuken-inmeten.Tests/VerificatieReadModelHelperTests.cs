using Keuken_inmeten.Models;
using Keuken_inmeten.Pages;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class VerificatieReadModelHelperTests
{
    [Fact]
    public void BouwPaginaModel_groepeert_taken_per_wand_en_kiest_eerste_open_taak_als_samenvatting()
    {
        var linkerwand = new KeukenWand { Id = Guid.NewGuid(), Naam = "Linkerwand" };
        var rechterwand = new KeukenWand { Id = Guid.NewGuid(), Naam = "Rechterwand" };

        var pagina = VerificatieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wanden: [linkerwand, rechterwand],
            paneelBronnen:
            [
                MaakPaneelBron(linkerwand.Id, linkerwand.Naam, kastNaam: "Kast links", matenOk: true, scharnierPositiesOk: true),
                MaakPaneelBron(rechterwand.Id, rechterwand.Naam, kastNaam: "Kast rechts", matenOk: false, scharnierPositiesOk: false)
            ],
            fase: VerificatieFase.Overzicht,
            paneelIndex: 0,
            laatsteGebruiktePotHartVanRand: ScharnierBerekeningService.CupCenterVanRand);

        var overzicht = Assert.IsType<VerificatieOverzichtModel>(pagina.Overzicht);
        Assert.Equal(1, overzicht.AantalAfgerond);
        Assert.Equal(1, overzicht.EersteOngecontroleerdIndex);
        Assert.Equal(2, overzicht.TaakGroepen.Count);
        Assert.True(overzicht.AlGestart);
        Assert.Equal("Kast rechts", overzicht.SamenvattingTaak.Resultaat.KastNaam);
        Assert.Equal(1, overzicht.TaakGroepen[0].AfgerondeTaken);
        Assert.Equal(1, overzicht.TaakGroepen[1].OpenTaken);
    }

    [Fact]
    public void BouwPaginaModel_bouwt_actief_paneel_met_boorgat_pothart_en_totalen()
    {
        var pagina = VerificatieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wanden: [],
            paneelBronnen:
            [
                MaakPaneelBron(Guid.NewGuid(), "Linkerwand", kastNaam: "Kast 1", matenOk: true, scharnierPositiesOk: true),
                MaakPaneelBron(Guid.NewGuid(), "Rechterwand", kastNaam: "Kast 2", matenOk: true, scharnierPositiesOk: false, boorgatX: 24.5, toewijzingPotHart: 21.5)
            ],
            fase: VerificatieFase.PaneelVerificatie,
            paneelIndex: 1,
            laatsteGebruiktePotHartVanRand: 19.5);

        var actiefPaneel = Assert.IsType<VerificatieActiefPaneelModel>(pagina.ActiefPaneel);
        Assert.Equal("Rechterwand", actiefPaneel.WandNaam);
        Assert.Equal(24.5, actiefPaneel.PotHartVanRand);
        Assert.Equal(1, actiefPaneel.AfgerondePanelen);
        Assert.Equal(1, actiefPaneel.OpenPaneelControles);
        Assert.Equal(2, actiefPaneel.TotaalPanelen);
        Assert.Equal(600, actiefPaneel.OpeningB);
        Assert.Equal(720, actiefPaneel.OpeningH);
        Assert.Single(actiefPaneel.PaneelKasten);
    }

    [Fact]
    public void BouwPaginaModel_filtert_overzicht_op_gefocuste_wand_zonder_andere_wanden_te_hernummeren()
    {
        var linkerwand = new KeukenWand { Id = Guid.NewGuid(), Naam = "Linkerwand" };
        var rechterwand = new KeukenWand { Id = Guid.NewGuid(), Naam = "Rechterwand" };

        var pagina = VerificatieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wanden: [linkerwand, rechterwand],
            paneelBronnen:
            [
                MaakPaneelBron(linkerwand.Id, linkerwand.Naam, kastNaam: "Kast links", matenOk: true, scharnierPositiesOk: true),
                MaakPaneelBron(rechterwand.Id, rechterwand.Naam, kastNaam: "Kast rechts", matenOk: false, scharnierPositiesOk: false)
            ],
            fase: VerificatieFase.Overzicht,
            paneelIndex: 0,
            laatsteGebruiktePotHartVanRand: ScharnierBerekeningService.CupCenterVanRand,
            gefocusteWandId: rechterwand.Id);

        var overzicht = Assert.IsType<VerificatieOverzichtModel>(pagina.Overzicht);
        var groep = Assert.Single(overzicht.TaakGroepen);
        Assert.Equal(rechterwand.Id, groep.WandId);
        Assert.Equal("Kast rechts", overzicht.SamenvattingTaak.Resultaat.KastNaam);
        Assert.Equal(1, overzicht.TotaalTaken);
        Assert.Equal(1, overzicht.EersteOngecontroleerdIndex);
        Assert.Equal(2, overzicht.TotaalOpenChecks);
        Assert.False(overzicht.AlGestart);
    }

    [Fact]
    public void BouwPaginaModel_gebruikt_laatste_pothart_als_geen_boorgat_of_toewijzing_bestaat()
    {
        var pagina = VerificatieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wanden: [],
            paneelBronnen:
            [
                MaakPaneelBron(Guid.NewGuid(), "Achterwand", type: PaneelType.BlindPaneel, boorgatX: null, toewijzingPotHart: null, matenOk: false)
            ],
            fase: VerificatieFase.PaneelVerificatie,
            paneelIndex: 0,
            laatsteGebruiktePotHartVanRand: 23.75);

        var actiefPaneel = Assert.IsType<VerificatieActiefPaneelModel>(pagina.ActiefPaneel);
        Assert.Equal(23.75, actiefPaneel.PotHartVanRand);
    }

    [Fact]
    public void BouwPaginaModel_bouwt_afronding_alleen_in_afrondingsfase()
    {
        VerificatiePaneelBron[] paneelBronnen =
        [
            MaakPaneelBron(Guid.NewGuid(), "Wand 1", matenOk: true, scharnierPositiesOk: true),
            MaakPaneelBron(Guid.NewGuid(), "Wand 2", matenOk: false, scharnierPositiesOk: false)
        ];

        var overzicht = VerificatieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wanden: [],
            paneelBronnen: paneelBronnen,
            fase: VerificatieFase.Overzicht,
            paneelIndex: 0,
            laatsteGebruiktePotHartVanRand: ScharnierBerekeningService.CupCenterVanRand);
        var afronding = VerificatieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wanden: [],
            paneelBronnen: paneelBronnen,
            fase: VerificatieFase.Afronding,
            paneelIndex: 0,
            laatsteGebruiktePotHartVanRand: ScharnierBerekeningService.CupCenterVanRand);

        Assert.Null(overzicht.Afronding);
        Assert.NotNull(afronding.Afronding);
        Assert.False(afronding.Afronding!.AlleGeverifieerd);
        Assert.Equal(1, afronding.Afronding.AantalGeverifieerd);
        Assert.Equal(2, afronding.Afronding.TotaalPanelen);
    }

    private static VerificatiePaneelBron MaakPaneelBron(
        Guid wandId,
        string wandNaam,
        string kastNaam = "Kast 1",
        PaneelType type = PaneelType.Deur,
        bool matenOk = false,
        bool scharnierPositiesOk = false,
        double breedte = 596,
        double hoogte = 716,
        double openingBreedte = 600,
        double openingHoogte = 720,
        double? boorgatX = 22.5,
        double? toewijzingPotHart = 22.5)
    {
        var toewijzingId = Guid.NewGuid();
        var kastId = Guid.NewGuid();
        var resultaat = new PaneelResultaat
        {
            ToewijzingId = toewijzingId,
            KastIds = [kastId],
            KastNaam = kastNaam,
            Type = type,
            Breedte = breedte,
            Hoogte = hoogte,
            MaatInfo = new PaneelMaatInfo
            {
                OpeningsRechthoek = new PaneelRechthoek
                {
                    Breedte = openingBreedte,
                    Hoogte = openingHoogte
                }
            },
            Boorgaten = type == PaneelType.Deur && boorgatX.HasValue
                ? [new Boorgat { X = boorgatX.Value, Y = 100 }]
                : []
        };
        var checks = new PaneelVerificatieStatus
        {
            ToewijzingId = toewijzingId,
            MatenOk = matenOk,
            ScharnierPositiesOk = scharnierPositiesOk
        };

        return new(
            Resultaat: resultaat,
            Checklist: VerificatieChecklistHelper.BouwStatus(resultaat, checks),
            Toewijzing: toewijzingPotHart.HasValue
                ? new PaneelToewijzing
                {
                    Id = toewijzingId,
                    PotHartVanRand = toewijzingPotHart.Value
                }
                : null,
            PaneelKasten:
            [
                new Kast
                {
                    Id = kastId,
                    Naam = kastNaam
                }
            ],
            WandId: wandId,
            WandNaam: wandNaam);
    }
}
