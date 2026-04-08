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

        var regel = Assert.Single(document.Regels);
        Assert.Equal("Hoge Deur 1", regel.Naam);
        Assert.Equal(2, regel.Aantal);
        Assert.Equal("Deur", regel.PaneelRolLabel);
        Assert.Equal(2200, regel.HoogteMm);
        Assert.Equal(600, regel.BreedteMm);
        Assert.Equal(BestellijstService.StandaardAbsBandLabel, regel.AbsBandLabel);
        Assert.Equal("Muur • Hoge kast links (+1 meer)", regel.ContextLabel);

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
        Assert.Contains("ABS-band", xml);
        Assert.Contains(BestellijstService.StandaardAbsBandLabel, xml);
        Assert.Contains(BestellijstExportService.CncNulpuntLabel, xml);
        Assert.Contains("Boorgat 1 X (links, mm)", xml);
        Assert.Contains("Boorgat 1 Y (boven, mm)", xml);
        Assert.Contains("577.5", xml);
        Assert.Contains("Hoge Deur 1", xml);
    }

    [Fact]
    public void Print_html_renderer_gebruikt_documentmodel_en_visual_renderer()
    {
        var document = MaakDocument();

        var html = BestellijstPrintHtmlRenderer.Render(document);

        Assert.Contains("Bestellijst", html);
        Assert.Contains("ABS-band", html);
        Assert.Contains(BestellijstService.StandaardAbsBandLabel, html);
        Assert.Contains(BestellijstExportService.CncNulpuntLabel, html);
        Assert.Contains("Boorgaten", html);
        Assert.Contains("B1: X", html);
        Assert.Contains("Y 83 mm", html);
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
        Assert.Contains("600 mm breed", svg);
        Assert.Contains("2200 mm hoog", svg);
        Assert.Contains("83 mm", svg);
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

        Assert.Equal("PDF met visuals", pdf.Label);
        Assert.Contains("printen", pdf.WanneerKiezen, StringComparison.OrdinalIgnoreCase);
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

        Assert.Contains(pdfPunten, punt => punt.Contains("visual", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(excelPunten, punt => punt.Contains("B3 X/Y", StringComparison.Ordinal));
        Assert.Contains(excelPunten, punt => punt.Contains("sorteren", StringComparison.OrdinalIgnoreCase));
    }
}

internal static class BestellijstExportTestData
{
    public static BestellijstItem MaakBestellijstItem() => new()
    {
        BasisNaam = "Hoge Deur",
        Naam = "Hoge Deur 1",
        Aantal = 2,
        AbsBandLabel = BestellijstService.StandaardAbsBandLabel,
        PaneelRolLabel = "Deur",
        WandNaam = "Muur",
        KastenLabel = "Hoge kast links",
        ContextLabel = "Muur • Hoge kast links (+1 meer)",
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
