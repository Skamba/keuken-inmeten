using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class AdZaagtExcelRendererTests
{
    [Fact]
    public void Render_bevat_adzaagt_templatestructuur_met_titel_kantcodes_en_kolomkoppen()
    {
        var document = MaakDocument();

        var xml = AdZaagtExcelRenderer.Render(document);

        Assert.Contains("INVULLIJST ZAAGSTAAT ADZAAGT", xml);
        Assert.Contains("Eindmaten invullen in mm, inclusief kantenband", xml);
        Assert.Contains("Kantcodes:", xml);
        Assert.Contains("1 = 1mm pvc kleur als basis materiaal", xml);
        Assert.Contains("10 = 1 mm kantfineer", xml);
        Assert.Contains("Orderinformatie:", xml);
        Assert.Contains("Naam klant:", xml);
        Assert.Contains("Referentie:", xml);
        Assert.Contains("Adres:", xml);
        Assert.Contains("Postcode:", xml);
        Assert.Contains("Plaats:", xml);
        Assert.Contains("Tel. nummer:", xml);
    }

    [Fact]
    public void Render_bevat_kolomkoppen_conform_adzaagt_template()
    {
        var document = MaakDocument();

        var xml = AdZaagtExcelRenderer.Render(document);

        Assert.Contains("Materiaal", xml);
        Assert.Contains("Color", xml);
        Assert.Contains("Dikte (mm)", xml);
        Assert.Contains("Nerf-richting ja/nee", xml);
        Assert.Contains("Aantal", xml);
        Assert.Contains("Breedte", xml);
        Assert.Contains("(Fineer-richting) Lengte", xml);
        Assert.Contains("Onderdeel", xml);
        Assert.Contains(">BB<", xml);
        Assert.Contains(">BO<", xml);
        Assert.Contains(">LR<", xml);
        Assert.Contains(">LL<", xml);
        Assert.Contains("Extra bewerkingen", xml);
    }

    [Fact]
    public void Render_bevat_dataregels_met_materiaal_afmetingen_en_kantcodes()
    {
        var document = MaakDocument();

        var xml = AdZaagtExcelRenderer.Render(document);

        Assert.Contains("MDF gelakt", xml);
        Assert.Contains("19", xml);
        Assert.Contains("600", xml);
        Assert.Contains("2200", xml);
        Assert.Contains("Hoge Deur 1", xml);
    }

    [Fact]
    public void Render_markeert_panelen_met_boorgaten_als_scharnierboringen()
    {
        var document = MaakDocument();

        var xml = AdZaagtExcelRenderer.Render(document);

        Assert.Contains("Scharnierboringen", xml);
    }

    [Fact]
    public void Render_laat_extra_bewerkingen_leeg_zonder_boorgaten()
    {
        var item = BestellijstExportTestData.MaakBestellijstItem();
        item.Boorgaten = [];
        item.Resultaat = new PaneelResultaat
        {
            KastNaam = "Kast",
            Type = PaneelType.BlindPaneel,
            Breedte = 600,
            Hoogte = 720,
            ScharnierZijde = ScharnierZijde.Links,
            Boorgaten = []
        };

        var document = BestellijstExportService.BouwDocument([item], "MDF gelakt", "19", new(2026, 4, 16, 10, 0, 0));

        var xml = AdZaagtExcelRenderer.Render(document);

        Assert.DoesNotContain("Scharnierboringen", xml);
    }

    [Fact]
    public void BepaalKantcode_geeft_code_1_voor_rondom()
    {
        Assert.Equal("1", AdZaagtExcelRenderer.BepaalKantcode("kantenband rondom"));
        Assert.Equal("1", AdZaagtExcelRenderer.BepaalKantcode("Kantenband Rondom"));
    }

    [Fact]
    public void BepaalKantcode_geeft_leeg_zonder_rondom()
    {
        Assert.Equal("", AdZaagtExcelRenderer.BepaalKantcode("geen kantenband"));
        Assert.Equal("", AdZaagtExcelRenderer.BepaalKantcode(""));
    }

    [Fact]
    public void Render_is_geldig_spreadsheetml()
    {
        var document = MaakDocument();

        var xml = AdZaagtExcelRenderer.Render(document);

        Assert.StartsWith("<?xml version=\"1.0\"?>", xml);
        Assert.Contains("progid=\"Excel.Sheet\"", xml);
        Assert.Contains("AdZaagt", xml);
        Assert.Contains("</Workbook>", xml);
    }

    [Fact]
    public void BouwAdZaagtExcelXml_delegeert_naar_renderer()
    {
        var item = BestellijstExportTestData.MaakBestellijstItem();

        var xml = BestellijstExportService.BouwAdZaagtExcelXml(
            [item], "MDF gelakt", "19", new(2026, 4, 16, 10, 0, 0));

        Assert.Contains("INVULLIJST ZAAGSTAAT ADZAAGT", xml);
        Assert.Contains("Hoge Deur 1", xml);
    }

    private static BestellijstExportDocument MaakDocument()
        => BestellijstExportService.BouwDocument(
            [BestellijstExportTestData.MaakBestellijstItem()],
            "MDF gelakt",
            "19",
            new(2026, 4, 16, 10, 0, 0));
}
