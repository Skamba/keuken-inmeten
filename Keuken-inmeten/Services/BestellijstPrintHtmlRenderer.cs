namespace Keuken_inmeten.Services;

using System.Text;
using Keuken_inmeten.Models;

public static class BestellijstPrintHtmlRenderer
{
    public static string Render(BestellijstExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"nl\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine($"  <title>{Encode(document.Titel)}</title>");
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
        sb.AppendLine($"  <h1>{Encode(document.Titel)}</h1>");
        sb.AppendLine($"  <div class=\"meta\"><span>Paneeltype: <strong>{Encode(document.PaneelType)}</strong></span><span>Dikte: <strong>{Encode(BestellijstExportFormatter.FormatDikteLabel(document.DikteMm))}</strong></span><span>CNC: <strong>{Encode(BestellijstExportFormatter.FormatCncAssenSamenvatting())}</strong></span><span>Gegenereerd: <strong>{Encode(BestellijstExportFormatter.FormatGeneratedAt(document.GeneratedAt))}</strong></span></div>");
        sb.AppendLine("  <div class=\"summary\">");
        sb.AppendLine($"    <div class=\"summary-card\"><span class=\"muted\">Orderregels</span><strong>{document.Orderregels}</strong></div>");
        sb.AppendLine($"    <div class=\"summary-card\"><span class=\"muted\">Totaal aantal</span><strong>{document.TotaalAantal}</strong></div>");
        sb.AppendLine($"    <div class=\"summary-card\"><span class=\"muted\">Totaal boorgaten</span><strong>{document.TotaalBoorgaten}</strong></div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <table>");
        sb.AppendLine("    <thead><tr><th>Naam</th><th>Aantal</th><th>Afmeting</th><th>ABS-band</th><th>Boorgaten</th><th>Visual</th></tr></thead>");
        sb.AppendLine("    <tbody>");

        foreach (var regel in document.Regels)
        {
            sb.AppendLine("      <tr>");
            sb.AppendLine($"        <td><div class=\"name\">{Encode(regel.Naam)}</div><div class=\"context\">{Encode(regel.ContextLabel)}</div></td>");
            sb.AppendLine($"        <td>{regel.Aantal}</td>");
            sb.AppendLine($"        <td>{Encode(regel.PaneelRolLabel)}<br />{Encode(BestellijstExportFormatter.FormatMm(regel.HoogteMm))} hoog<br />{Encode(BestellijstExportFormatter.FormatMm(regel.BreedteMm))} breed</td>");
            sb.AppendLine($"        <td>{Encode(regel.AbsBandLabel)}</td>");
            sb.AppendLine($"        <td class=\"holes\">{BuildHoleLines(regel)}</td>");
            sb.AppendLine($"        <td class=\"visual\">{BestellijstVisualRenderer.Render(regel.Visual)}</td>");
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

    private static string BuildHoleLines(BestellijstExportRegel regel)
    {
        if (regel.Boorgaten.Count == 0)
            return "<span class=\"muted\">Geen boorgaten</span>";

        var lines = regel.Boorgaten.Select(boorgat =>
            $"<span class=\"muted\">B{boorgat.Nummer}: X {Encode(BestellijstExportFormatter.FormatMm(boorgat.XCncMm))}, Y {Encode(BestellijstExportFormatter.FormatMm(boorgat.YCncMm))}</span>");

        return string.Join("<br />", lines);
    }

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
