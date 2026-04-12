namespace Keuken_inmeten.Services;

using System.Text;
using Keuken_inmeten.Models;

public static class BestellijstVisualRenderer
{
    public static string Render(BestellijstVisualDocument document)
    {
        const double svgWidth = 176;
        const double svgHeight = 200;
        const double panelAreaTop = 24;
        const double panelAreaHeight = 126;
        const double panelAreaWidth = 126;
        const double regelBadgeWidth = 36;
        const double footerSizeY = 187;
        const double footerAxisY = 196;
        const double drillLabelPaddingX = 3.5;
        const double drillLabelHeight = 11;
        const double drillLabelGap = 8;
        const double panelInnerLabelMargin = 3;
        const double panelOuterLabelGap = 8;
        var scale = Math.Min(
            panelAreaWidth / Math.Max(document.BreedteMm, 1),
            panelAreaHeight / Math.Max(document.HoogteMm, 1));
        var width = document.BreedteMm * scale;
        var height = document.HoogteMm * scale;
        var paneelX = (svgWidth - width) / 2;
        var paneelY = panelAreaTop + ((panelAreaHeight - height) / 2);
        var oorsprongLabelY = Math.Max(9, paneelY - 4);
        var sb = new StringBuilder();

        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Fmt(svgWidth)}\" height=\"{Fmt(svgHeight)}\" viewBox=\"0 0 {Fmt(svgWidth)} {Fmt(svgHeight)}\">");
        sb.Append($"<rect x=\"{Fmt(0.5)}\" y=\"{Fmt(0.5)}\" width=\"{Fmt(svgWidth - 1)}\" height=\"{Fmt(svgHeight - 1)}\" fill=\"#f8fafc\" stroke=\"#d8e1ea\" stroke-width=\"1\" rx=\"6\" />");
        sb.Append($"<text x=\"{Fmt(12)}\" y=\"{Fmt(13)}\" font-size=\"7.4\" fill=\"#334155\" text-anchor=\"start\">Bovenzijde</text>");
        sb.Append($"<rect x=\"{Fmt(svgWidth - regelBadgeWidth - 6)}\" y=\"{Fmt(4)}\" width=\"{Fmt(regelBadgeWidth)}\" height=\"14\" rx=\"7\" fill=\"#0f4c81\" opacity=\"0.9\" />");
        sb.Append($"<text x=\"{Fmt(svgWidth - regelBadgeWidth / 2 - 6)}\" y=\"{Fmt(13.5)}\" font-size=\"7\" fill=\"#fff\" text-anchor=\"middle\" font-weight=\"700\">{Encode(document.RegelCode)}</text>");
        sb.Append($"<rect x=\"{Fmt(paneelX)}\" y=\"{Fmt(paneelY)}\" width=\"{Fmt(width)}\" height=\"{Fmt(height)}\" fill=\"#dce6f0\" stroke=\"#5b7ea1\" stroke-width=\"2\" rx=\"4\" />");
        sb.Append($"<circle cx=\"{Fmt(paneelX)}\" cy=\"{Fmt(paneelY)}\" r=\"2.6\" fill=\"#0f4c81\" />");
        sb.Append($"<text x=\"{Fmt(paneelX)}\" y=\"{Fmt(oorsprongLabelY)}\" font-size=\"6.8\" fill=\"#0f4c81\" text-anchor=\"middle\">0,0</text>");

        if (document.Boorgaten.Count > 0)
        {
            var badgeY = panelAreaTop + panelAreaHeight + 6;
            var gewensteScharnierBadgeX = document.ScharnierZijde == ScharnierZijde.Links ? paneelX : paneelX + width - 58;
            var scharnierBadgeX = Math.Max(10, Math.Min(gewensteScharnierBadgeX, svgWidth - 68));
            sb.Append($"<rect x=\"{Fmt(scharnierBadgeX)}\" y=\"{Fmt(badgeY)}\" width=\"58\" height=\"12\" rx=\"6\" fill=\"#e2ecf7\" stroke=\"#9bb5cf\" />");
            sb.Append($"<text x=\"{Fmt(scharnierBadgeX + 29)}\" y=\"{Fmt(badgeY + 8.2)}\" font-size=\"6.8\" fill=\"#23415b\" text-anchor=\"middle\" font-weight=\"600\">Scharnier {Encode(document.ScharnierZijde.ToString().ToLowerInvariant())}</text>");
        }

        for (var i = 0; i < document.Boorgaten.Count; i++)
        {
            var boorgat = document.Boorgaten[i];
            var cx = document.ScharnierZijde == ScharnierZijde.Links
                ? paneelX + boorgat.XVanScharnierzijdeMm * scale
                : paneelX + width - boorgat.XVanScharnierzijdeMm * scale;
            var cy = paneelY + boorgat.YVanafBovenMm * scale;
            var radius = Math.Max((boorgat.DiameterMm / 2.0) * scale, 4);
            var labelText = $"#{i + 1} · {BestellijstExportFormatter.FormatMm(boorgat.YVanafBovenMm)}";
            var anchor = document.ScharnierZijde == ScharnierZijde.Links ? "start" : "end";
            var labelWidth = EstimateDrillLabelWidth(labelText, drillLabelPaddingX);
            var gewensteLabelRectX = document.ScharnierZijde == ScharnierZijde.Links
                ? cx + radius + drillLabelGap
                : cx - radius - drillLabelGap - labelWidth;
            var pastBinnenPaneel = gewensteLabelRectX >= paneelX + panelInnerLabelMargin
                && gewensteLabelRectX + labelWidth <= paneelX + width - panelInnerLabelMargin;
            var labelRectX = pastBinnenPaneel
                ? gewensteLabelRectX
                : document.ScharnierZijde == ScharnierZijde.Links
                    ? paneelX + width + panelOuterLabelGap
                    : paneelX - panelOuterLabelGap - labelWidth;
            var labelRectY = cy - (drillLabelHeight / 2.0);

            labelRectX = Math.Max(6, Math.Min(labelRectX, svgWidth - labelWidth - 6));
            labelRectY = Math.Max(6, Math.Min(labelRectY, svgHeight - drillLabelHeight - 6));

            var labelTextX = anchor == "start"
                ? labelRectX + drillLabelPaddingX
                : labelRectX + labelWidth - drillLabelPaddingX;
            var labelTextY = labelRectY + (drillLabelHeight / 2.0) + 0.5;

            sb.Append($"<circle cx=\"{Fmt(cx)}\" cy=\"{Fmt(cy)}\" r=\"{Fmt(radius)}\" fill=\"#243746\" opacity=\"0.82\" />");
            sb.Append($"<text x=\"{Fmt(cx)}\" y=\"{Fmt(cy + 2.4)}\" font-size=\"6.5\" fill=\"#fff\" text-anchor=\"middle\" font-weight=\"700\">{i + 1}</text>");
            sb.Append($"<rect x=\"{Fmt(labelRectX)}\" y=\"{Fmt(labelRectY)}\" width=\"{Fmt(labelWidth)}\" height=\"{Fmt(drillLabelHeight)}\" rx=\"5.5\" fill=\"#ffffff\" stroke=\"#cbd5e1\" stroke-width=\"0.75\" />");
            sb.Append($"<text x=\"{Fmt(labelTextX)}\" y=\"{Fmt(labelTextY)}\" font-size=\"7.2\" fill=\"#334155\" text-anchor=\"{anchor}\" dominant-baseline=\"middle\" font-weight=\"600\">{Encode(labelText)}</text>");
        }

        sb.Append($"<text x=\"{Fmt(svgWidth / 2)}\" y=\"{Fmt(footerSizeY)}\" font-size=\"7.6\" fill=\"#334155\" text-anchor=\"middle\">{Encode(BestellijstExportFormatter.FormatZaagmaat(document.BreedteMm, document.HoogteMm))}</text>");
        sb.Append($"<text x=\"{Fmt(svgWidth / 2)}\" y=\"{Fmt(footerAxisY)}\" font-size=\"7\" fill=\"#64748b\" text-anchor=\"middle\">X links · Y boven</text>");
        sb.Append("</svg>");

        return sb.ToString();
    }

    private static string Fmt(double value) => VisualisatieHelper.FmtData(value);

    private static double EstimateDrillLabelWidth(string labelText, double paddingX)
        => Math.Max(32, (labelText.Length * 4.1) + (paddingX * 2));

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
