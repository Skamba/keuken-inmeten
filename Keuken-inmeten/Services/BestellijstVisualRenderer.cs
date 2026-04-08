namespace Keuken_inmeten.Services;

using System.Text;
using Keuken_inmeten.Models;

public static class BestellijstVisualRenderer
{
    public static string Render(BestellijstVisualDocument document)
    {
        const double padding = 26;
        const double maxHoogte = 150;
        var scale = maxHoogte / Math.Max(document.HoogteMm, 1);
        var width = document.BreedteMm * scale;
        var height = document.HoogteMm * scale;
        var svgWidth = width + padding * 2;
        var svgHeight = height + padding * 2;
        var sb = new StringBuilder();

        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Fmt(svgWidth)}\" height=\"{Fmt(svgHeight)}\" viewBox=\"0 0 {Fmt(svgWidth)} {Fmt(svgHeight)}\">");
        sb.Append($"<rect x=\"{Fmt(padding)}\" y=\"{Fmt(padding)}\" width=\"{Fmt(width)}\" height=\"{Fmt(height)}\" fill=\"#dce6f0\" stroke=\"#5b7ea1\" stroke-width=\"2\" rx=\"4\" />");

        foreach (var boorgat in document.Boorgaten)
        {
            var cx = document.ScharnierZijde == ScharnierZijde.Links
                ? padding + boorgat.XVanScharnierzijdeMm * scale
                : padding + width - boorgat.XVanScharnierzijdeMm * scale;
            var cy = padding + boorgat.YVanafBovenMm * scale;
            var radius = Math.Max((boorgat.DiameterMm / 2.0) * scale, 4);
            var labelX = document.ScharnierZijde == ScharnierZijde.Links ? cx + radius + 6 : cx - radius - 6;
            var anchor = document.ScharnierZijde == ScharnierZijde.Links ? "start" : "end";

            sb.Append($"<circle cx=\"{Fmt(cx)}\" cy=\"{Fmt(cy)}\" r=\"{Fmt(radius)}\" fill=\"#243746\" opacity=\"0.82\" />");
            sb.Append($"<text x=\"{Fmt(labelX)}\" y=\"{Fmt(cy + 3)}\" font-size=\"8\" fill=\"#4b5563\" text-anchor=\"{anchor}\">{Encode(BestellijstExportFormatter.FormatMm(boorgat.YVanafBovenMm))}</text>");
        }

        sb.Append($"<text x=\"{Fmt(svgWidth / 2)}\" y=\"{Fmt(16)}\" font-size=\"9\" fill=\"#334155\" text-anchor=\"middle\">{Encode(BestellijstExportFormatter.FormatMm(document.BreedteMm))} breed</text>");
        sb.Append($"<text x=\"{Fmt(12)}\" y=\"{Fmt(svgHeight / 2)}\" font-size=\"9\" fill=\"#334155\" text-anchor=\"middle\" transform=\"rotate(-90, {Fmt(12)}, {Fmt(svgHeight / 2)})\">{Encode(BestellijstExportFormatter.FormatMm(document.HoogteMm))} hoog</text>");
        sb.Append("</svg>");

        return sb.ToString();
    }

    private static string Fmt(double value) => VisualisatieHelper.FmtData(value);

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
