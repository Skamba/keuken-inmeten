namespace Keuken_inmeten.Services;

using System.Text;
using Keuken_inmeten.Models;

public static class BestellijstVisualRenderer
{
    public static string Render(BestellijstVisualDocument document)
    {
        const double horizontalPadding = 20;
        const double headerHeight = 20;
        const double footerHeight = 38;
        const double minCanvasWidth = 176;
        const double regelBadgeWidth = 36;
        const double maxHoogte = 130;
        var scale = maxHoogte / Math.Max(document.HoogteMm, 1);
        var width = document.BreedteMm * scale;
        var height = document.HoogteMm * scale;
        var svgWidth = Math.Max(width + horizontalPadding * 2, minCanvasWidth);
        var svgHeight = height + headerHeight + footerHeight + 12;
        var paneelX = (svgWidth - width) / 2;
        var paneelY = headerHeight;
        var sb = new StringBuilder();

        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Fmt(svgWidth)}\" height=\"{Fmt(svgHeight)}\" viewBox=\"0 0 {Fmt(svgWidth)} {Fmt(svgHeight)}\">");
        sb.Append($"<rect x=\"{Fmt(0.5)}\" y=\"{Fmt(0.5)}\" width=\"{Fmt(svgWidth - 1)}\" height=\"{Fmt(svgHeight - 1)}\" fill=\"#f8fafc\" stroke=\"#d8e1ea\" stroke-width=\"1\" rx=\"6\" />");
        sb.Append($"<text x=\"{Fmt(12)}\" y=\"{Fmt(13)}\" font-size=\"7.4\" fill=\"#334155\" text-anchor=\"start\">Bovenzijde</text>");
        sb.Append($"<rect x=\"{Fmt(svgWidth - regelBadgeWidth - 6)}\" y=\"{Fmt(4)}\" width=\"{Fmt(regelBadgeWidth)}\" height=\"14\" rx=\"7\" fill=\"#0f4c81\" opacity=\"0.9\" />");
        sb.Append($"<text x=\"{Fmt(svgWidth - regelBadgeWidth / 2 - 6)}\" y=\"{Fmt(13.5)}\" font-size=\"7\" fill=\"#fff\" text-anchor=\"middle\" font-weight=\"700\">{Encode(document.RegelCode)}</text>");
        sb.Append($"<rect x=\"{Fmt(paneelX)}\" y=\"{Fmt(paneelY)}\" width=\"{Fmt(width)}\" height=\"{Fmt(height)}\" fill=\"#dce6f0\" stroke=\"#5b7ea1\" stroke-width=\"2\" rx=\"4\" />");
        sb.Append($"<circle cx=\"{Fmt(paneelX)}\" cy=\"{Fmt(paneelY)}\" r=\"2.6\" fill=\"#0f4c81\" />");
        sb.Append($"<text x=\"{Fmt(paneelX + 7)}\" y=\"{Fmt(paneelY + 3)}\" font-size=\"6.8\" fill=\"#0f4c81\" text-anchor=\"start\">0,0</text>");

        if (document.Boorgaten.Count > 0)
        {
            var gewensteScharnierBadgeX = document.ScharnierZijde == ScharnierZijde.Links ? paneelX : paneelX + width - 58;
            var scharnierBadgeX = Math.Max(10, Math.Min(gewensteScharnierBadgeX, svgWidth - 68));
            sb.Append($"<rect x=\"{Fmt(scharnierBadgeX)}\" y=\"{Fmt(paneelY + height + 6)}\" width=\"58\" height=\"12\" rx=\"6\" fill=\"#e2ecf7\" stroke=\"#9bb5cf\" />");
            sb.Append($"<text x=\"{Fmt(scharnierBadgeX + 29)}\" y=\"{Fmt(paneelY + height + 14.2)}\" font-size=\"6.8\" fill=\"#23415b\" text-anchor=\"middle\" font-weight=\"600\">Scharnier {Encode(document.ScharnierZijde.ToString().ToLowerInvariant())}</text>");
        }

        for (var i = 0; i < document.Boorgaten.Count; i++)
        {
            var boorgat = document.Boorgaten[i];
            var cx = document.ScharnierZijde == ScharnierZijde.Links
                ? paneelX + boorgat.XVanScharnierzijdeMm * scale
                : paneelX + width - boorgat.XVanScharnierzijdeMm * scale;
            var cy = paneelY + boorgat.YVanafBovenMm * scale;
            var radius = Math.Max((boorgat.DiameterMm / 2.0) * scale, 4);
            var labelX = document.ScharnierZijde == ScharnierZijde.Links ? cx + radius + 6 : cx - radius - 6;
            var anchor = document.ScharnierZijde == ScharnierZijde.Links ? "start" : "end";

            sb.Append($"<circle cx=\"{Fmt(cx)}\" cy=\"{Fmt(cy)}\" r=\"{Fmt(radius)}\" fill=\"#243746\" opacity=\"0.82\" />");
            sb.Append($"<text x=\"{Fmt(cx)}\" y=\"{Fmt(cy + 2.4)}\" font-size=\"6.5\" fill=\"#fff\" text-anchor=\"middle\" font-weight=\"700\">{i + 1}</text>");
            sb.Append($"<text x=\"{Fmt(labelX)}\" y=\"{Fmt(cy + 3)}\" font-size=\"7.2\" fill=\"#4b5563\" text-anchor=\"{anchor}\">#{i + 1} · {Encode(BestellijstExportFormatter.FormatMm(boorgat.YVanafBovenMm))}</text>");
        }

        sb.Append($"<text x=\"{Fmt(svgWidth / 2)}\" y=\"{Fmt(svgHeight - 13)}\" font-size=\"7.6\" fill=\"#334155\" text-anchor=\"middle\">{Encode(BestellijstExportFormatter.FormatZaagmaat(document.BreedteMm, document.HoogteMm))}</text>");
        sb.Append($"<text x=\"{Fmt(svgWidth / 2)}\" y=\"{Fmt(svgHeight - 4)}\" font-size=\"7\" fill=\"#64748b\" text-anchor=\"middle\">X links · Y boven</text>");
        sb.Append("</svg>");

        return sb.ToString();
    }

    private static string Fmt(double value) => VisualisatieHelper.FmtData(value);

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
