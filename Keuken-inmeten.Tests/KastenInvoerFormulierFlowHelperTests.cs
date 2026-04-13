namespace Keuken_inmeten.Tests;

using Keuken_inmeten.Models;
using Keuken_inmeten.Pages;
using Xunit;

public class KastenInvoerFormulierFlowHelperTests
{
    [Fact]
    public void BouwKastFormulierModel_geeft_stapinfo_en_blokkeert_volgende_zonder_wand_of_naam()
    {
        var kast = MaakKast();

        var model = KastenInvoerFormulierFlowHelper.BouwKastFormulierModel(
            stap: 1,
            actieveWandId: null,
            actieveWandNaam: "Geen wand gekozen",
            formKast: kast,
            toonTechnischeInstellingen: false,
            technischeControleBevestigd: false);

        Assert.Equal("Basis", model.Stap.Label);
        Assert.Equal("Geen wand gekozen", model.ActieveWandNaam);
        Assert.False(model.Stap.KanNaarVolgendeStap);
    }

    [Fact]
    public void BouwKastFormulierModel_geeft_afwijkende_technische_controle_bij_aangepaste_waarden()
    {
        var kast = MaakKast();
        kast.Wanddikte = 19;

        var model = KastenInvoerFormulierFlowHelper.BouwKastFormulierModel(
            stap: 3,
            actieveWandId: Guid.NewGuid(),
            actieveWandNaam: "Achterwand",
            formKast: kast,
            toonTechnischeInstellingen: true,
            technischeControleBevestigd: true);

        Assert.True(model.Stap.KanNaarVolgendeStap);
        Assert.True(model.ToonTechnischeInstellingen);
        Assert.Contains("deze technische waarden", model.TechnischeControleCheckboxLabel);
        Assert.Contains("Gecontroleerd: 19 mm wanddikte", model.TechnischeControleSamenvatting);
    }

    [Fact]
    public void BouwApparaatFormulierModel_geeft_matenstap_en_blokkeert_volgende_bij_onvolledige_maatvoering()
    {
        var apparaat = new Apparaat
        {
            Naam = "Oven",
            Type = ApparaatType.Oven,
            Breedte = 600,
            Hoogte = 0,
            Diepte = 560
        };

        var model = KastenInvoerFormulierFlowHelper.BouwApparaatFormulierModel(
            stap: 2,
            actieveWandNaam: "Zijwand",
            formApparaat: apparaat);

        Assert.Equal("Maten", model.Stap.Label);
        Assert.Equal("Zijwand", model.ActieveWandNaam);
        Assert.False(model.Stap.KanNaarVolgendeStap);
    }

    [Fact]
    public void BouwApparaatFormulierModel_geeft_controle_stapinfo_voor_laatste_stap()
    {
        var apparaat = new Apparaat
        {
            Naam = "Koelkast",
            Type = ApparaatType.Koelkast,
            Breedte = 600,
            Hoogte = 1780,
            Diepte = 560
        };

        var model = KastenInvoerFormulierFlowHelper.BouwApparaatFormulierModel(
            stap: 3,
            actieveWandNaam: "Kastenwand",
            formApparaat: apparaat);

        Assert.Equal(3, model.Stap.LaatsteStap);
        Assert.Equal("Controle", model.Stap.Label);
        Assert.Equal("Controleer de samenvatting en voorvertoning voordat u het apparaat opslaat.", model.Stap.Intro);
        Assert.False(model.Stap.KanNaarVolgendeStap);
    }

    private static Kast MaakKast()
        => new()
        {
            Naam = "Onderkast",
            Breedte = 600,
            Hoogte = 720,
            Diepte = 560,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19
        };
}
