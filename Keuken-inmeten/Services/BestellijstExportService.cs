namespace Keuken_inmeten.Services;

using System.Globalization;
using System.Text;
using Keuken_inmeten.Models;

public static class BestellijstExportService
{
    public static string BouwExcelXml(IReadOnlyList<BestellijstItem> items, string paneelType, string dikteMm, DateTime generatedAt)
    {
        var maxBoorgaten = items.Count == 0 ? 0 : items.Max(item => item.Boorgaten.Count);
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

        AppendRow(sb, new[]
        {
            CellText("Bestellijst", "Titel")
        });
        AppendRow(sb, new[]
        {
            CellText("Paneeltype", "Meta"),
            CellText(paneelType)
        });
        AppendRow(sb, new[]
        {
            CellText("Dikte (mm)", "Meta"),
            CellText(FormatDikteLabel(dikteMm))
        });
        AppendRow(sb, new[]
        {
            CellText("Gegenereerd op", "Meta"),
            CellText(generatedAt.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))
        });
        AppendRow(sb, Array.Empty<string>());

        var koppen = new List<string>
        {
            CellText("Naam", "Kop"),
            CellText("Aantal", "Kop"),
            CellText("Paneelrol", "Kop"),
            CellText("Hoogte (mm)", "Kop"),
            CellText("Breedte (mm)", "Kop"),
            CellText("ABS-band", "Kop"),
            CellText("Context", "Kop")
        };

        for (int i = 0; i < maxBoorgaten; i++)
        {
            var nummer = i + 1;
            koppen.Add(CellText($"Boorgat {nummer} X (mm)", "Kop"));
            koppen.Add(CellText($"Boorgat {nummer} Y (mm)", "Kop"));
        }

        AppendRow(sb, koppen);

        foreach (var item in items)
        {
            var row = new List<string>
            {
                CellText(item.Naam),
                CellNumber(item.Aantal),
                CellText(item.PaneelRolLabel),
                CellNumber(item.Hoogte),
                CellNumber(item.Breedte),
                CellText(item.AbsBandLabel),
                CellText(item.ContextLabel)
            };

            for (int i = 0; i < maxBoorgaten; i++)
            {
                if (i < item.Boorgaten.Count)
                {
                    row.Add(CellNumber(item.Boorgaten[i].X));
                    row.Add(CellNumber(item.Boorgaten[i].Y));
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

    public static string BouwPdfHtml(IReadOnlyList<BestellijstItem> items, string paneelType, string dikteMm, DateTime generatedAt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"nl\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <title>Bestellijst</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    @page { size: A4 portrait; margin: 14mm; }");
        sb.AppendLine("    body { font-family: 'Segoe UI', Arial, sans-serif; color: #1f2937; margin: 0; }");
        sb.AppendLine("    h1 { font-size: 20px; margin: 0 0 8px; }");
        sb.AppendLine("    p { margin: 0; }");
        sb.AppendLine("    .meta { display: flex; gap: 18px; flex-wrap: wrap; margin-bottom: 18px; font-size: 12px; color: #475569; }");
        sb.AppendLine("    .summary { display: flex; gap: 10px; margin-bottom: 16px; flex-wrap: wrap; }");
        sb.AppendLine("    .summary-card { border: 1px solid #d8e1ea; border-radius: 10px; padding: 10px 12px; min-width: 110px; }");
        sb.AppendLine("    .summary-card strong { display: block; font-size: 18px; color: #0f4c81; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; font-size: 11px; }");
        sb.AppendLine("    th, td { border: 1px solid #d9e1ea; padding: 8px; vertical-align: top; }");
        sb.AppendLine("    th { background: #edf4fb; text-align: left; font-size: 10px; text-transform: uppercase; letter-spacing: 0.03em; }");
        sb.AppendLine("    .name { font-weight: 700; font-size: 12px; margin-bottom: 4px; }");
        sb.AppendLine("    .context { color: #5b6470; font-size: 10px; line-height: 1.4; }");
        sb.AppendLine("    .visual { width: 170px; }");
        sb.AppendLine("    .holes { line-height: 1.5; }");
        sb.AppendLine("    .muted { color: #5b6470; }");
        sb.AppendLine("    tr { page-break-inside: avoid; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <h1>Bestellijst</h1>");
        sb.AppendLine($"  <div class=\"meta\"><span>Paneeltype: <strong>{Encode(paneelType)}</strong></span><span>Dikte: <strong>{Encode(FormatDikteLabel(dikteMm))}</strong></span><span>Gegenereerd: <strong>{Encode(generatedAt.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))}</strong></span></div>");
        sb.AppendLine("  <div class=\"summary\">");
        sb.AppendLine($"    <div class=\"summary-card\"><span class=\"muted\">Orderregels</span><strong>{items.Count}</strong></div>");
        sb.AppendLine($"    <div class=\"summary-card\"><span class=\"muted\">Totaal aantal</span><strong>{items.Sum(item => item.Aantal)}</strong></div>");
        sb.AppendLine($"    <div class=\"summary-card\"><span class=\"muted\">Totaal boorgaten</span><strong>{items.Sum(item => item.Boorgaten.Count * item.Aantal)}</strong></div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <table>");
        sb.AppendLine("    <thead><tr><th>Naam</th><th>Aantal</th><th>Afmeting</th><th>ABS-band</th><th>Boorgaten</th><th>Visual</th></tr></thead>");
        sb.AppendLine("    <tbody>");

        foreach (var item in items)
        {
            sb.AppendLine("      <tr>");
            sb.AppendLine($"        <td><div class=\"name\">{Encode(item.Naam)}</div><div class=\"context\">{Encode(item.ContextLabel)}</div></td>");
            sb.AppendLine($"        <td>{item.Aantal}</td>");
            sb.AppendLine($"        <td>{Encode(item.PaneelRolLabel)}<br />{Encode(FormatMm(item.Hoogte))} hoog<br />{Encode(FormatMm(item.Breedte))} breed</td>");
            sb.AppendLine($"        <td>{Encode(item.AbsBandLabel)}</td>");
            sb.AppendLine($"        <td class=\"holes\">{BuildHoleLines(item)}</td>");
            sb.AppendLine($"        <td class=\"visual\">{BuildVisualSvg(item.Resultaat)}</td>");
            sb.AppendLine("      </tr>");
        }

        sb.AppendLine("    </tbody>");
        sb.AppendLine("  </table>");
        sb.AppendLine("  <script>");
        sb.AppendLine("    window.addEventListener('load', function () {");
        sb.AppendLine("      setTimeout(function () { window.focus(); window.print(); }, 120);");
        sb.AppendLine("    });");
        sb.AppendLine("  </script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    public static string MaakBestandsNaam(string prefix, string paneelType, string extensie, DateTime generatedAt)
    {
        var paneelSlug = Slugify(string.IsNullOrWhiteSpace(paneelType) ? "paneel" : paneelType);
        return $"{prefix}-{paneelSlug}-{generatedAt:yyyyMMdd-HHmm}.{extensie.TrimStart('.')}";
    }

    private static string BuildVisualSvg(PaneelResultaat resultaat)
    {
        const double padding = 26;
        const double maxHoogte = 150;
        var scale = maxHoogte / Math.Max(resultaat.Hoogte, 1);
        var width = resultaat.Breedte * scale;
        var height = resultaat.Hoogte * scale;
        var svgWidth = width + padding * 2;
        var svgHeight = height + padding * 2;
        var sb = new StringBuilder();

        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Fmt(svgWidth)}\" height=\"{Fmt(svgHeight)}\" viewBox=\"0 0 {Fmt(svgWidth)} {Fmt(svgHeight)}\">");
        sb.Append($"<rect x=\"{Fmt(padding)}\" y=\"{Fmt(padding)}\" width=\"{Fmt(width)}\" height=\"{Fmt(height)}\" fill=\"#dce6f0\" stroke=\"#5b7ea1\" stroke-width=\"2\" rx=\"4\" />");

        foreach (var boorgat in resultaat.Boorgaten.OrderBy(boorgat => boorgat.Y))
        {
            var cx = resultaat.ScharnierZijde == ScharnierZijde.Links
                ? padding + boorgat.X * scale
                : padding + width - boorgat.X * scale;
            var cy = padding + boorgat.Y * scale;
            var radius = Math.Max((boorgat.Diameter / 2.0) * scale, 4);
            var labelX = resultaat.ScharnierZijde == ScharnierZijde.Links ? cx + radius + 6 : cx - radius - 6;
            var anchor = resultaat.ScharnierZijde == ScharnierZijde.Links ? "start" : "end";

            sb.Append($"<circle cx=\"{Fmt(cx)}\" cy=\"{Fmt(cy)}\" r=\"{Fmt(radius)}\" fill=\"#243746\" opacity=\"0.82\" />");
            sb.Append($"<text x=\"{Fmt(labelX)}\" y=\"{Fmt(cy + 3)}\" font-size=\"8\" fill=\"#4b5563\" text-anchor=\"{anchor}\">{Encode(FormatMm(boorgat.Y))}</text>");
        }

        sb.Append($"<text x=\"{Fmt(svgWidth / 2)}\" y=\"{Fmt(16)}\" font-size=\"9\" fill=\"#334155\" text-anchor=\"middle\">{Encode(FormatMm(resultaat.Breedte))} breed</text>");
        sb.Append($"<text x=\"{Fmt(12)}\" y=\"{Fmt(svgHeight / 2)}\" font-size=\"9\" fill=\"#334155\" text-anchor=\"middle\" transform=\"rotate(-90, {Fmt(12)}, {Fmt(svgHeight / 2)})\">{Encode(FormatMm(resultaat.Hoogte))} hoog</text>");
        sb.Append("</svg>");

        return sb.ToString();
    }

    private static string BuildHoleLines(BestellijstItem item)
    {
        if (item.Boorgaten.Count == 0)
            return "<span class=\"muted\">Geen boorgaten</span>";

        var lines = item.Boorgaten
            .Select((boorgat, index) => $"<span class=\"muted\">B{index + 1}: X {Encode(FormatMm(boorgat.X))}, Y {Encode(FormatMm(boorgat.Y))}</span>");

        return string.Join("<br />", lines);
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

    public static string FormatDikteLabel(string dikteMm)
    {
        if (string.IsNullOrWhiteSpace(dikteMm))
            return "n.t.b.";

        var trimmed = dikteMm.Trim();
        return trimmed.Contains("mm", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed} mm";
    }

    public static string FormatMm(double value) => $"{value:0.#} mm";

    private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);

    private static string Slugify(string value)
    {
        var slug = new StringBuilder();
        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                slug.Append(character);
            }
            else if (slug.Length == 0 || slug[^1] == '-')
            {
                continue;
            }
            else
            {
                slug.Append('-');
            }
        }

        return slug.ToString().Trim('-');
    }
}
