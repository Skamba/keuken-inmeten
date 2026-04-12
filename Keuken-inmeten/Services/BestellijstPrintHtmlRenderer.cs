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
        sb.AppendLine("    @page { size: A4 landscape; margin: 12mm 10mm 14mm; }");
        sb.AppendLine("    body { font-family: 'Segoe UI', Arial, sans-serif; color: #1f2937; margin: 0; font-size: 10px; }");
        sb.AppendLine("    h1 { font-size: 20px; margin: 0; }");
        sb.AppendLine("    p { margin: 0; }");
        sb.AppendLine("    .document-header { display: flex; justify-content: space-between; align-items: flex-end; gap: 16px; margin-bottom: 10px; }");
        sb.AppendLine("    .document-subtitle { color: #475569; font-size: 11px; max-width: 520px; line-height: 1.45; }");
        sb.AppendLine("    .meta-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 8px; margin-bottom: 12px; }");
        sb.AppendLine("    .meta-chip { border: 1px solid #d8e1ea; border-radius: 10px; padding: 8px 10px; background: #f8fafc; }");
        sb.AppendLine("    .meta-chip-label { display: block; color: #64748b; font-size: 8px; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 4px; }");
        sb.AppendLine("    .meta-chip-value { font-weight: 700; font-size: 11px; color: #0f172a; }");
        sb.AppendLine("    .summary { display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; }");
        sb.AppendLine("    .summary-card { border: 1px solid #d8e1ea; border-radius: 10px; padding: 8px 10px; min-width: 112px; background: #fff; }");
        sb.AppendLine("    .summary-card strong { display: block; font-size: 16px; color: #0f4c81; margin-top: 2px; }");
        sb.AppendLine("    .summary-card span { color: #64748b; font-size: 8px; text-transform: uppercase; letter-spacing: 0.05em; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; table-layout: fixed; font-size: 9.4px; }");
        sb.AppendLine("    thead { display: table-header-group; }");
        sb.AppendLine("    th, td { border: 1px solid #d9e1ea; padding: 6px; vertical-align: top; }");
        sb.AppendLine("    th { background: #edf4fb; text-align: left; font-size: 8.2px; text-transform: uppercase; letter-spacing: 0.05em; }");
        sb.AppendLine("    .repeat-meta th { background: #f8fafc; color: #475569; text-transform: none; letter-spacing: 0; font-size: 8.3px; }");
        sb.AppendLine("    .col-regel { width: 7%; }");
        sb.AppendLine("    .col-paneel { width: 27%; }");
        sb.AppendLine("    .col-aantal { width: 6%; }");
        sb.AppendLine("    .col-maat { width: 16%; }");
        sb.AppendLine("    .col-boorbeeld { width: 22%; }");
        sb.AppendLine("    .col-visual { width: 22%; }");
        sb.AppendLine("    .rule-code { font-weight: 800; font-size: 14px; color: #0f4c81; margin-bottom: 3px; }");
        sb.AppendLine("    .name { font-weight: 700; font-size: 10.6px; margin-bottom: 3px; }");
        sb.AppendLine("    .sub-meta { color: #334155; font-size: 8.6px; line-height: 1.35; margin-bottom: 4px; }");
        sb.AppendLine("    .context-title { color: #64748b; font-size: 8px; text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 3px; }");
        sb.AppendLine("    .locations { margin: 0; padding-left: 14px; color: #5b6470; font-size: 8.7px; line-height: 1.35; }");
        sb.AppendLine("    .locations li { margin: 0 0 2px; }");
        sb.AppendLine("    .qty { text-align: center; font-size: 14px; font-weight: 800; color: #0f172a; }");
        sb.AppendLine("    .cut-size { font-weight: 700; font-size: 10.8px; color: #0f172a; margin-bottom: 4px; }");
        sb.AppendLine("    .measure-meta { color: #5b6470; font-size: 8.7px; line-height: 1.35; }");
        sb.AppendLine("    .drill-summary { font-weight: 700; color: #0f172a; margin-bottom: 3px; }");
        sb.AppendLine("    .drill-reference { color: #64748b; font-size: 8.1px; margin-bottom: 4px; }");
        sb.AppendLine("    .drill-table { width: 100%; border-collapse: collapse; font-size: 8.5px; }");
        sb.AppendLine("    .drill-table th, .drill-table td { border: none; padding: 1px 0; }");
        sb.AppendLine("    .drill-table thead th { background: none; border-bottom: 1px solid #d9e1ea; font-size: 7.8px; text-transform: none; letter-spacing: 0; color: #475569; padding-bottom: 2px; }");
        sb.AppendLine("    .drill-table td:first-child, .drill-table th:first-child { width: 18px; }");
        sb.AppendLine("    .visual { width: 180px; }");
        sb.AppendLine("    .muted { color: #64748b; }");
        sb.AppendLine("    tr { page-break-inside: avoid; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"document-header\">");
        sb.AppendLine("    <div>");
        sb.AppendLine($"      <h1>{Encode(document.Titel)}</h1>");
        sb.AppendLine("      <p class=\"document-subtitle\">Werkplaatsversie voor zaagbedrijf of CNC-voorbereiding. Elke orderregel bundelt code, bronlocaties, zaagmaat, boorbeeld en visualisatie.</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine($"    <div class=\"muted\">Gegenereerd: <strong>{Encode(BestellijstExportFormatter.FormatGeneratedAt(document.GeneratedAt))}</strong></div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"meta-grid\">");
        sb.AppendLine($"    <div class=\"meta-chip\"><span class=\"meta-chip-label\">Materiaal</span><span class=\"meta-chip-value\">{Encode(document.PaneelType)}</span></div>");
        sb.AppendLine($"    <div class=\"meta-chip\"><span class=\"meta-chip-label\">Dikte</span><span class=\"meta-chip-value\">{Encode(BestellijstExportFormatter.FormatDikteLabel(document.DikteMm))}</span></div>");
        sb.AppendLine($"    <div class=\"meta-chip\"><span class=\"meta-chip-label\">CNC referentie</span><span class=\"meta-chip-value\">{Encode(BestellijstExportFormatter.FormatCncAssenSamenvatting())}</span></div>");
        sb.AppendLine($"    <div class=\"meta-chip\"><span class=\"meta-chip-label\">Totaal oppervlak</span><span class=\"meta-chip-value\">{Encode(BestellijstExportFormatter.FormatVierkanteMeter(document.TotaalOppervlakteM2))}</span></div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"summary\">");
        sb.AppendLine($"    <div class=\"summary-card\"><span>Orderregels</span><strong>{document.Orderregels}</strong></div>");
        sb.AppendLine($"    <div class=\"summary-card\"><span>Totaal panelen</span><strong>{document.TotaalAantal}</strong></div>");
        sb.AppendLine($"    <div class=\"summary-card\"><span>Totaal 35 mm potscharniergaten</span><strong>{document.TotaalBoorgaten}</strong></div>");
        sb.AppendLine($"    <div class=\"summary-card\"><span>Totaal oppervlak</span><strong>{Encode(BestellijstExportFormatter.FormatVierkanteMeter(document.TotaalOppervlakteM2))}</strong></div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <table>");
        sb.AppendLine("    <thead>");
        sb.AppendLine($"      <tr class=\"repeat-meta\"><th colspan=\"6\">Materiaal {Encode(document.PaneelType)} · {Encode(BestellijstExportFormatter.FormatDikteLabel(document.DikteMm))} · {Encode(BestellijstExportFormatter.FormatCncAssenSamenvatting())} · {Encode(BestellijstExportFormatter.FormatVierkanteMeter(document.TotaalOppervlakteM2))} totaal oppervlak</th></tr>");
        sb.AppendLine("      <tr><th class=\"col-regel\">Regel</th><th class=\"col-paneel\">Paneel en bronlocaties</th><th class=\"col-aantal\">Aantal</th><th class=\"col-maat\">Zaagmaat en materiaal</th><th class=\"col-boorbeeld\">Boorbeeld / CNC</th><th class=\"col-visual\">Visual</th></tr>");
        sb.AppendLine("    </thead>");
        sb.AppendLine("    <tbody>");

        foreach (var regel in document.Regels)
        {
            sb.AppendLine("      <tr>");
            sb.AppendLine($"        <td><div class=\"rule-code\">{Encode(regel.RegelCode)}</div><div class=\"muted\">Regel {regel.RegelNummer}</div></td>");
            sb.AppendLine($"        <td><div class=\"name\">{Encode(regel.Naam)}</div><div class=\"sub-meta\">{Encode(regel.PaneelRolLabel)} · {Encode(regel.KantenbandLabel)}{BuildScharnierMeta(regel)}</div>{BuildLocationList(regel)}</td>");
            sb.AppendLine($"        <td class=\"qty\">{regel.Aantal}</td>");
            sb.AppendLine($"        <td><div class=\"cut-size\">{Encode(BestellijstExportFormatter.FormatZaagmaat(regel.BreedteMm, regel.HoogteMm))}</div><div class=\"measure-meta\">{Encode(BestellijstExportFormatter.FormatVierkanteMeter(regel.OppervlaktePerStukM2))} per stuk</div><div class=\"measure-meta\">{Encode(BestellijstExportFormatter.FormatVierkanteMeter(regel.TotaleOppervlakteM2))} totaal</div><div class=\"measure-meta\">{Encode(document.PaneelType)} · {Encode(BestellijstExportFormatter.FormatDikteLabel(document.DikteMm))}</div></td>");
            sb.AppendLine($"        <td>{BuildHoleBlock(document, regel)}</td>");
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

    private static string BuildLocationList(BestellijstExportRegel regel)
    {
        IEnumerable<string> locaties = regel.BronLocaties.Count > 0 ? regel.BronLocaties : [regel.ContextLabel];
        var sb = new StringBuilder();
        sb.Append("<div class=\"context-title\">Bronlocaties</div>");
        sb.Append("<ul class=\"locations\">");
        foreach (var locatie in locaties.Where(locatie => !string.IsNullOrWhiteSpace(locatie)))
            sb.Append($"<li>{Encode(locatie)}</li>");
        sb.Append("</ul>");
        return sb.ToString();
    }

    private static string BuildHoleBlock(BestellijstExportDocument document, BestellijstExportRegel regel)
    {
        if (regel.Boorgaten.Count == 0)
            return "<div class=\"muted\">Geen boorwerk voor deze orderregel.</div>";

        var sb = new StringBuilder();
        sb.Append($"<div class=\"drill-summary\">{Encode(BestellijstExportFormatter.FormatBoorbeeldSamenvatting(regel.ScharnierLabel, regel.Boorgaten.Count))}</div>");
        sb.Append($"<div class=\"drill-reference\">{Encode(document.CncNulpuntLabel)} · {Encode(document.CncXAsLabel)} · {Encode(document.CncYAsLabel)}</div>");
        sb.Append("<table class=\"drill-table\"><thead><tr><th>#</th><th>X (mm)</th><th>Y (mm)</th></tr></thead><tbody>");
        foreach (var boorgat in regel.Boorgaten)
            sb.Append($"<tr><td>{boorgat.Nummer}</td><td>{Encode(BestellijstExportFormatter.FormatMm(boorgat.XCncMm))}</td><td>{Encode(BestellijstExportFormatter.FormatMm(boorgat.YCncMm))}</td></tr>");
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string BuildScharnierMeta(BestellijstExportRegel regel)
        => regel.Boorgaten.Count == 0 || string.IsNullOrWhiteSpace(regel.ScharnierLabel) || regel.ScharnierLabel == "—"
            ? string.Empty
            : $" · scharnier {Encode(regel.ScharnierLabel.ToLowerInvariant())}";

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
