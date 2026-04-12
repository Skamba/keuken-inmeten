namespace Keuken_inmeten.Services;

using System.Text;
using Keuken_inmeten.Models;

public static class BestellijstExcelRenderer
{
    public static string Render(BestellijstExportDocument document)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
        sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
        sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        sb.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("  <Styles>");
        sb.AppendLine("    <Style ss:ID=\"Titel\"><Font ss:Bold=\"1\" ss:Size=\"14\" /></Style>");
        sb.AppendLine("    <Style ss:ID=\"Kop\"><Font ss:Bold=\"1\" /><Interior ss:Color=\"#DCE6F0\" ss:Pattern=\"Solid\" /></Style>");
        sb.AppendLine("    <Style ss:ID=\"Meta\"><Font ss:Color=\"#555555\" /></Style>");
        sb.AppendLine("  </Styles>");
        sb.AppendLine("  <Worksheet ss:Name=\"Bestellijst\">");
        sb.AppendLine("    <Table>");

        AppendRow(sb, [CellText(document.Titel, "Titel")]);
        AppendRow(sb,
        [
            CellText("Paneeltype", "Meta"),
            CellText(document.PaneelType)
        ]);
        AppendRow(sb,
        [
            CellText("Dikte (mm)", "Meta"),
            CellText(BestellijstExportFormatter.FormatDikteLabel(document.DikteMm))
        ]);
        AppendRow(sb,
        [
            CellText("CNC nulpunt", "Meta"),
            CellText(document.CncNulpuntLabel)
        ]);
        AppendRow(sb,
        [
            CellText("X-as", "Meta"),
            CellText(document.CncXAsLabel)
        ]);
        AppendRow(sb,
        [
            CellText("Y-as", "Meta"),
            CellText(document.CncYAsLabel)
        ]);
        AppendRow(sb,
        [
            CellText("Boortype", "Meta"),
            CellText("35 mm potscharniergaten")
        ]);
        AppendRow(sb,
        [
            CellText("Totaal oppervlak (m²)", "Meta"),
            CellNumber(document.TotaalOppervlakteM2)
        ]);
        AppendRow(sb,
        [
            CellText("Gegenereerd op", "Meta"),
            CellText(BestellijstExportFormatter.FormatGeneratedAt(document.GeneratedAt))
        ]);
        AppendRow(sb, Array.Empty<string>());

        var koppen = new List<string>
        {
            CellText("Regel", "Kop"),
            CellText("Naam", "Kop"),
            CellText("Aantal", "Kop"),
            CellText("Paneelrol", "Kop"),
            CellText("Scharnierzijde", "Kop"),
            CellText("Eindmaat na kantenband B × H (mm)", "Kop"),
            CellText("Oppervlakte per stuk (m²)", "Kop"),
            CellText("Oppervlakte totaal (m²)", "Kop"),
            CellText("Hoogte (mm)", "Kop"),
            CellText("Breedte (mm)", "Kop"),
            CellText("Kantenband", "Kop"),
            CellText("Context", "Kop"),
            CellText("Bronlocaties", "Kop")
        };

        for (var i = 0; i < document.MaxBoorgaten; i++)
        {
            var nummer = i + 1;
            koppen.Add(CellText($"Potscharniergat {nummer} X (links, mm)", "Kop"));
            koppen.Add(CellText($"Potscharniergat {nummer} Y (boven, mm)", "Kop"));
        }

        AppendRow(sb, koppen);

        foreach (var regel in document.Regels)
        {
            var row = new List<string>
            {
                CellText(regel.RegelCode),
                CellText(regel.Naam),
                CellNumber(regel.Aantal),
                CellText(regel.PaneelRolLabel),
                CellText(regel.ScharnierLabel),
                CellText(BestellijstExportFormatter.FormatZaagmaat(regel.BreedteMm, regel.HoogteMm)),
                CellNumber(regel.OppervlaktePerStukM2),
                CellNumber(regel.TotaleOppervlakteM2),
                CellNumber(regel.HoogteMm),
                CellNumber(regel.BreedteMm),
                CellText(regel.KantenbandLabel),
                CellText(regel.ContextLabel),
                CellText(BestellijstExportFormatter.FormatBronLocaties(regel.BronLocaties))
            };

            for (var i = 0; i < document.MaxBoorgaten; i++)
            {
                if (i < regel.Boorgaten.Count)
                {
                    row.Add(CellNumber(regel.Boorgaten[i].XCncMm));
                    row.Add(CellNumber(regel.Boorgaten[i].YCncMm));
                }
                else
                {
                    row.Add(CellText(""));
                    row.Add(CellText(""));
                }
            }

            AppendRow(sb, row);
        }

        sb.AppendLine("    </Table>");
        sb.AppendLine("  </Worksheet>");
        sb.AppendLine("</Workbook>");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, IEnumerable<string> cells)
    {
        sb.AppendLine("      <Row>");
        foreach (var cell in cells)
            sb.AppendLine($"        {cell}");
        sb.AppendLine("      </Row>");
    }

    private static string CellText(string value, string? styleId = null)
        => styleId is null
            ? $"<Cell><Data ss:Type=\"String\">{Encode(value)}</Data></Cell>"
            : $"<Cell ss:StyleID=\"{styleId}\"><Data ss:Type=\"String\">{Encode(value)}</Data></Cell>";

    private static string CellNumber(double value, string? styleId = null)
        => styleId is null
            ? $"<Cell><Data ss:Type=\"Number\">{Fmt(value)}</Data></Cell>"
            : $"<Cell ss:StyleID=\"{styleId}\"><Data ss:Type=\"Number\">{Fmt(value)}</Data></Cell>";

    private static string CellNumber(int value, string? styleId = null)
        => styleId is null
            ? $"<Cell><Data ss:Type=\"Number\">{value}</Data></Cell>"
            : $"<Cell ss:StyleID=\"{styleId}\"><Data ss:Type=\"Number\">{value}</Data></Cell>";

    private static string Fmt(double value) => VisualisatieHelper.FmtData(value);

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
