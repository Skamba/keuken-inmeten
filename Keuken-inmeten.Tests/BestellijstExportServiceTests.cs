using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class BestellijstExportDocumentTests
{
    [Fact]
    public void BouwDocument_levert_typed_exportdata()
    {
        var item = BestellijstExportTestData.MaakBestellijstItem();

        var document = BestellijstExportService.BouwDocument(
            [item],
            "MDF gelakt",
            "19",
            new DateTime(2026, 4, 6, 14, 0, 0));

        Assert.Equal("Bestellijst", document.Titel);
        Assert.Equal("MDF gelakt", document.PaneelType);
        Assert.Equal("19", document.DikteMm);
        Assert.Equal(1, document.Orderregels);
        Assert.Equal(2, document.TotaalAantal);
        Assert.Equal(6, document.TotaalBoorgaten);
        Assert.Equal(3, document.MaxBoorgaten);
        Assert.Equal(2.64, document.TotaalOppervlakteM2);

        var regel = Assert.Single(document.Regels);
        Assert.Equal(1, regel.RegelNummer);
        Assert.Equal("R01", regel.RegelCode);
        Assert.Equal("Hoge Deur 1", regel.Naam);
        Assert.Equal(2, regel.Aantal);
        Assert.Equal("Deur", regel.PaneelRolLabel);
        Assert.Equal("Rechts", regel.ScharnierLabel);
        Assert.Equal(2200, regel.HoogteMm);
        Assert.Equal(600, regel.BreedteMm);
        Assert.Equal(1.32, regel.OppervlaktePerStukM2);
        Assert.Equal(2.64, regel.TotaleOppervlakteM2);
        Assert.Equal(BestellijstService.StandaardKantenbandLabel, regel.KantenbandLabel);
        Assert.Equal("Muur • Hoge kast links (+1 meer)", regel.ContextLabel);
        Assert.Equal(
            ["Muur • Hoge kast links", "Muur • Hoge kast rechts"],
            regel.BronLocaties);

        Assert.Collection(
            regel.Boorgaten,
            eerste =>
            {
                Assert.Equal(1, eerste.Nummer);
                Assert.Equal(577.5, eerste.XCncMm);
                Assert.Equal(83, eerste.YCncMm);
            },
            _ => { },
            _ => { });

        Assert.Equal("R01", regel.Visual.RegelCode);
        Assert.Equal(600, regel.Visual.BreedteMm);
        Assert.Equal(2200, regel.Visual.HoogteMm);
        Assert.Equal(ScharnierZijde.Rechts, regel.Visual.ScharnierZijde);
        Assert.Equal(3, regel.Visual.Boorgaten.Count);
    }
}

public class BestellijstRenderersTests
{
    [Fact]
    public void Excel_renderer_gebruikt_documentmodel_voor_metadata_en_boorgatkolommen()
    {
        var document = MaakDocument();

        var xml = BestellijstExcelRenderer.Render(document);

        Assert.Contains("Paneeltype", xml);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("Totaal oppervlak (m²)"), xml);
        Assert.Contains("Regel", xml);
        Assert.Contains("Kantenband", xml);
        Assert.Contains(BestellijstService.StandaardKantenbandLabel, xml);
        Assert.Contains(BestellijstExportService.CncNulpuntLabel, xml);
        Assert.Contains("Boortype", xml);
        Assert.Contains("35 mm potscharniergaten", xml);
        Assert.Contains("Bronlocaties", xml);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("Zaagmaat B × H (mm)"), xml);
        Assert.Contains("Potscharniergat 1 X (links, mm)", xml);
        Assert.Contains("Potscharniergat 1 Y (boven, mm)", xml);
        Assert.Contains("577.5", xml);
        Assert.Contains("R01", xml);
        Assert.Contains("2.64", xml);
        Assert.Contains("Hoge Deur 1", xml);
        Assert.Contains("Hoge kast links", xml);
        Assert.Contains("Hoge kast rechts", xml);
    }

    [Fact]
    public void Print_html_renderer_gebruikt_documentmodel_en_visual_renderer()
    {
        var document = MaakDocument();

        var html = BestellijstPrintHtmlRenderer.Render(document);

        Assert.Contains("Bestellijst", html);
        Assert.Contains("R01", html);
        Assert.Contains(BestellijstExportService.CncNulpuntLabel, html);
        Assert.Contains("Bronlocaties", html);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("Muur • Hoge kast rechts"), html);
        Assert.Contains(System.Net.WebUtility.HtmlEncode(BestellijstExportFormatter.FormatZaagmaat(600, 2200)), html);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("2.64 m² totaal"), html);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("Scharnier rechts · 3 potscharniergaten"), html);
        Assert.Contains("<th>#</th><th>X (mm)</th><th>Y (mm)</th>", html);
        Assert.Contains("83 mm", html);
        Assert.Contains("Hoge Deur 1", html);
        Assert.Contains("<svg", html);
        Assert.Contains("window.print()", html);
    }

    [Fact]
    public void Visual_renderer_rendert_svg_los_van_bestellijst_html()
    {
        var document = MaakDocument();

        var svg = BestellijstVisualRenderer.Render(document.Regels[0].Visual);

        Assert.Contains("<svg", svg);
        Assert.Contains("R01", svg);
        Assert.Contains("Bovenzijde · nulpunt linksboven", svg);
        Assert.Contains(System.Net.WebUtility.HtmlEncode(BestellijstExportFormatter.FormatZaagmaat(600, 2200)), svg);
        Assert.Contains("#1 · 83 mm", svg);
    }

    private static BestellijstExportDocument MaakDocument()
        => BestellijstExportService.BouwDocument(
            [BestellijstExportTestData.MaakBestellijstItem()],
            "MDF gelakt",
            "19",
            new DateTime(2026, 4, 6, 14, 0, 0));
}

public class BestellijstExportFlowHelperTests
{
    [Fact]
    public void Voor_geeft_taakgerichte_uitleg_per_exporttype()
    {
        var pdf = BestellijstExportFlowHelper.Voor(BestellijstExportType.Pdf);
        var excel = BestellijstExportFlowHelper.Voor(BestellijstExportType.Excel);

        Assert.Equal("PDF met visualisaties", pdf.Label);
        Assert.Contains("zaagbedrijf", pdf.WanneerKiezen, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Open printweergave", pdf.BevestigLabel);

        Assert.Equal("Excel alleen lijst", excel.Label);
        Assert.Contains("sorteren", excel.WanneerKiezen, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Download Excel", excel.BevestigLabel);
    }

    [Fact]
    public void MaakPreviewPunten_noemt_formatspecifieke_previewinformatie()
    {
        var document = BestellijstExportService.BouwDocument(
            [BestellijstExportTestData.MaakBestellijstItem()],
            "MDF gelakt",
            "19",
            new DateTime(2026, 4, 6, 14, 0, 0));

        var pdfPunten = BestellijstExportFlowHelper.MaakPreviewPunten(document, BestellijstExportType.Pdf);
        var excelPunten = BestellijstExportFlowHelper.MaakPreviewPunten(document, BestellijstExportType.Excel);

        Assert.Contains(pdfPunten, punt => punt.Contains("bronlocaties", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pdfPunten, punt => punt.Contains("2.64 m²", StringComparison.Ordinal));
        Assert.Contains(excelPunten, punt => punt.Contains("1 t/m 3", StringComparison.Ordinal));
        Assert.Contains(excelPunten, punt => punt.Contains("bronlocaties", StringComparison.OrdinalIgnoreCase));
    }
}

internal static class BestellijstExportTestData
{
    public static BestellijstItem MaakBestellijstItem() => new()
    {
        BasisNaam = "Hoge Deur",
        Naam = "Hoge Deur 1",
        Aantal = 2,
        KantenbandLabel = BestellijstService.StandaardKantenbandLabel,
        PaneelRolLabel = "Deur",
        WandNaam = "Muur",
        KastenLabel = "Hoge kast links",
        ContextLabel = "Muur • Hoge kast links (+1 meer)",
        BronLocaties = ["Muur • Hoge kast links", "Muur • Hoge kast rechts"],
        ScharnierLabel = "Rechts",
        Hoogte = 2200,
        Breedte = 600,
        Resultaat = new PaneelResultaat
        {
            KastNaam = "Hoge kast links",
            Type = PaneelType.Deur,
            Breedte = 600,
            Hoogte = 2200,
            ScharnierZijde = ScharnierZijde.Rechts,
            Boorgaten =
            [
                new Boorgat { X = 22.5, Y = 83 },
                new Boorgat { X = 22.5, Y = 606 },
                new Boorgat { X = 22.5, Y = 1118 }
            ]
        },
        Boorgaten =
        [
            new Boorgat { X = 22.5, Y = 83 },
            new Boorgat { X = 22.5, Y = 606 },
            new Boorgat { X = 22.5, Y = 1118 }
        ]
    };
}
