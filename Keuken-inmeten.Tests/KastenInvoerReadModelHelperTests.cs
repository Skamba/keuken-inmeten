namespace Keuken_inmeten.Tests;

using Keuken_inmeten.Pages;
using Keuken_inmeten.Models;
using Xunit;

public class KastenInvoerReadModelHelperTests
{
    [Fact]
    public void BouwPaginaModel_bouwt_overzicht_samenvatting_met_badges_en_meta()
    {
        var wand = MaakWand("Achterwand", breedte: 2400);
        var kast = MaakKast("Onderkast", breedte: 600);
        var apparaat = MaakApparaat("Oven");
        wand.KastIds = [kast.Id];
        wand.ApparaatIds = [apparaat.Id];

        var model = KastenInvoerReadModelHelper.BouwPaginaModel(
            [wand],
            [kast],
            [apparaat],
            actieveWandId: null,
            toonKastFormulier: false,
            toonApparaatFormulier: false);

        Assert.Null(model.ActieveWerkruimte);

        var overzicht = Assert.Single(model.OverzichtWanden);
        Assert.Equal("600 mm ingevuld · 1800 mm vrije wandruimte", overzicht.OverzichtMetaTekst);
        Assert.Equal("600 mm ingevuld · 1800 mm vrij · 2400 mm wandbreedte", overzicht.SchakelaarMetaTekst);
        Assert.Collection(
            overzicht.OverzichtBadges,
            badge => Assert.Equal("1 kast(en)", badge.Label),
            badge => Assert.Equal("1 apparaat(en)", badge.Label),
            badge => Assert.Equal("2400 mm", badge.Label));
    }

    [Fact]
    public void BouwPaginaModel_filtert_actieve_wand_uit_overzicht_en_geeft_eerste_kast_stap()
    {
        var actieveWand = MaakWand("Achterwand");
        var andereWand = MaakWand("Zijwand");
        var andereKast = MaakKast("Kolomkast", breedte: 500);
        andereWand.KastIds = [andereKast.Id];

        var model = KastenInvoerReadModelHelper.BouwPaginaModel(
            [actieveWand, andereWand],
            [andereKast],
            [],
            actieveWand.Id,
            toonKastFormulier: false,
            toonApparaatFormulier: false);

        var actieveWerkruimte = Assert.IsType<KastenInvoerActieveWerkruimteModel>(model.ActieveWerkruimte);
        Assert.Equal(actieveWand.Id, actieveWerkruimte.Samenvatting.Wand.Id);
        Assert.True(actieveWerkruimte.ToonPrimaireActies);
        Assert.False(actieveWerkruimte.ToonWandOpstelling);
        Assert.Equal("Nu doen", actieveWerkruimte.Werkstap.Kicker);
        Assert.Equal("Voeg eerst de eerste kast toe.", actieveWerkruimte.Werkstap.Titel);

        var overzicht = Assert.Single(model.OverzichtWanden);
        Assert.Equal(andereWand.Id, overzicht.Wand.Id);
    }

    [Fact]
    public void BouwPaginaModel_verbergt_primaire_acties_tijdens_open_formulier_en_toont_vervolgstap()
    {
        var wand = MaakWand("Achterwand");
        var kast = MaakKast("Onderkast");
        var apparaat = MaakApparaat("Oven");
        wand.KastIds = [kast.Id];
        wand.ApparaatIds = [apparaat.Id];

        var model = KastenInvoerReadModelHelper.BouwPaginaModel(
            [wand],
            [kast],
            [apparaat],
            wand.Id,
            toonKastFormulier: true,
            toonApparaatFormulier: false);

        var actieveWerkruimte = Assert.IsType<KastenInvoerActieveWerkruimteModel>(model.ActieveWerkruimte);
        Assert.False(actieveWerkruimte.ToonPrimaireActies);
        Assert.True(actieveWerkruimte.ToonWandOpstelling);
        Assert.Equal("Volgende stap", actieveWerkruimte.Werkstap.Kicker);
        Assert.Null(actieveWerkruimte.Werkstap.Titel);
        Assert.Contains("Werk alleen bij wat op deze wand nog ontbreekt.", actieveWerkruimte.Werkstap.Beschrijving);
    }

    private static KeukenWand MaakWand(string naam, double breedte = 3000)
        => new()
        {
            Id = Guid.NewGuid(),
            Naam = naam,
            Breedte = breedte,
            Hoogte = 2600,
            PlintHoogte = 100
        };

    private static Kast MaakKast(string naam, double breedte = 600)
        => new()
        {
            Id = Guid.NewGuid(),
            Naam = naam,
            Breedte = breedte,
            Hoogte = 720,
            Diepte = 560
        };

    private static Apparaat MaakApparaat(string naam)
        => new()
        {
            Id = Guid.NewGuid(),
            Naam = naam,
            Type = ApparaatType.Oven,
            Breedte = 600,
            Hoogte = 600,
            Diepte = 560
        };
}
