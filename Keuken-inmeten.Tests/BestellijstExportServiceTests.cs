using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using System.Globalization;
using System.Text.RegularExpressions;
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
        Assert.Contains(System.Net.WebUtility.HtmlEncode("Eindmaat na kantenband B × H (mm)"), xml);
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
    public void Pdf_payload_builder_gebruikt_documentmodel_en_visual_renderer()
    {
        var document = MaakDocument();

        var payload = BestellijstPdfPayloadBuilder.Bouw(document);
        var regel = Assert.Single(payload.Regels);

        Assert.Equal("Bestellijst", payload.Titel);
        Assert.Equal("MDF gelakt", payload.PaneelType);
        Assert.Equal("19 mm", payload.DikteLabel);
        Assert.Equal(BestellijstExportFormatter.FormatCncAssenSamenvatting(), payload.CncReferentieLabel);
        Assert.Equal(BestellijstExportFormatter.FormatVierkanteMeter(2.64), payload.TotaalOppervlakteLabel);
        Assert.Equal("R01", regel.RegelCode);
        Assert.Equal("Hoge Deur 1", regel.Naam);
        Assert.Contains("scharnier rechts", regel.PaneelMeta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Muur • Hoge kast rechts", regel.BronLocaties);
        Assert.Equal(BestellijstExportFormatter.FormatZaagmaat(600, 2200), regel.ZaagmaatLabel);
        Assert.Equal(BestellijstExportFormatter.FormatVierkanteMeter(1.32), regel.OppervlaktePerStukLabel);
        Assert.Equal(BestellijstExportFormatter.FormatVierkanteMeter(2.64), regel.TotaleOppervlakteLabel);
        Assert.Equal("Scharnier rechts · 3 potscharniergaten", regel.BoorbeeldSamenvatting);
        Assert.Collection(
            regel.Boorgaten,
            eerste =>
            {
                Assert.Equal(1, eerste.Nummer);
                Assert.Equal(BestellijstExportFormatter.FormatMm(577.5), eerste.XCncLabel);
                Assert.Equal(BestellijstExportFormatter.FormatMm(83), eerste.YCncLabel);
            },
            _ => { },
            _ => { });
        Assert.Contains("<svg", regel.VisualSvg);
    }

    [Fact]
    public void Visual_renderer_rendert_svg_los_van_bestellijst_html()
    {
        var document = MaakDocument();

        var svg = BestellijstVisualRenderer.Render(document.Regels[0].Visual);

        Assert.Contains("<svg", svg);
        Assert.Contains("R01", svg);
        Assert.Contains("Bovenzijde", svg);
        Assert.Contains(System.Net.WebUtility.HtmlEncode(BestellijstExportFormatter.FormatZaagmaat(600, 2200)), svg);
        Assert.Contains("X links · Y boven", svg);
        Assert.Contains("text-anchor=\"middle\">0,0</text>", svg);
        Assert.DoesNotContain("text-anchor=\"start\">0,0</text>", svg);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("#1 · 83 mm"), svg);
    }

    [Fact]
    public void Visual_renderer_houdt_brede_panelen_binnen_vaste_kaartbreedte()
    {
        var document = new BestellijstVisualDocument(
            "R99",
            1198,
            168,
            ScharnierZijde.Links,
            []);

        var svg = BestellijstVisualRenderer.Render(document);

        Assert.Contains("width=\"176\"", svg);
        Assert.Contains("height=\"200\"", svg);
        Assert.Contains(System.Net.WebUtility.HtmlEncode(BestellijstExportFormatter.FormatZaagmaat(1198, 168)), svg);
        Assert.Contains("X links · Y boven", svg);
    }

    [Fact]
    public void Visual_renderer_geeft_boorgatlabels_een_goed_leesbare_achtergrond_op_smalle_panelen()
    {
        var document = new BestellijstVisualDocument(
            "R03",
            598,
            2228,
            ScharnierZijde.Rechts,
            [
                new BestellijstVisualBoorgat(22.5, 92, 35),
                new BestellijstVisualBoorgat(22.5, 601, 35),
                new BestellijstVisualBoorgat(22.5, 1113, 35),
                new BestellijstVisualBoorgat(22.5, 1657, 35),
                new BestellijstVisualBoorgat(22.5, 2137, 35)
            ]);

        var svg = BestellijstVisualRenderer.Render(document);
        var paneelMatch = Regex.Match(
            svg,
            "<rect x=\\\"(?<x>[0-9.]+)\\\" y=\\\"(?<y>[0-9.]+)\\\" width=\\\"(?<width>[0-9.]+)\\\" height=\\\"(?<height>[0-9.]+)\\\" fill=\\\"#dce6f0\\\" stroke=\\\"#5b7ea1\\\"");
        var labelMatch = Regex.Match(
            svg,
            "<rect x=\\\"(?<x>[0-9.]+)\\\" y=\\\"(?<y>[0-9.]+)\\\" width=\\\"(?<width>[0-9.]+)\\\" height=\\\"11\\\" rx=\\\"5.5\\\" fill=\\\"#ffffff\\\" stroke=\\\"#cbd5e1\\\"");

        Assert.Contains(System.Net.WebUtility.HtmlEncode("#1 · 92 mm"), svg);
        Assert.Contains("fill=\"#ffffff\" stroke=\"#cbd5e1\"", svg);
        Assert.Equal(5, Regex.Matches(svg, "fill=\\\"#ffffff\\\" stroke=\\\"#cbd5e1\\\"").Count);
        Assert.True(paneelMatch.Success);
        Assert.True(labelMatch.Success);

        var paneelX = double.Parse(paneelMatch.Groups["x"].Value, CultureInfo.InvariantCulture);
        var labelX = double.Parse(labelMatch.Groups["x"].Value, CultureInfo.InvariantCulture);
        var labelWidth = double.Parse(labelMatch.Groups["width"].Value, CultureInfo.InvariantCulture);

        Assert.True(labelX + labelWidth <= paneelX, "Verwacht dat de labelbox volledig links buiten het paneel valt.");
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
        Assert.Equal("Download PDF", pdf.BevestigLabel);

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
        Assert.Contains(pdfPunten, punt => punt.Contains(BestellijstExportFormatter.FormatVierkanteMeter(2.64), StringComparison.Ordinal));
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
