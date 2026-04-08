namespace Keuken_inmeten.Services;

using System.Globalization;

public static class BestellijstExportFormatter
{
    public static string FormatCncAssenSamenvatting()
        => $"{BestellijstExportService.CncNulpuntLabel} · {BestellijstExportService.CncXAsLabel} · {BestellijstExportService.CncYAsLabel}";

    public static string FormatDikteLabel(string dikteMm)
    {
        if (string.IsNullOrWhiteSpace(dikteMm))
            return "n.t.b.";

        var trimmed = dikteMm.Trim();
        return trimmed.Contains("mm", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed} mm";
    }

    public static string FormatGeneratedAt(DateTime generatedAt)
        => generatedAt.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string FormatMm(double value) => $"{value:0.#} mm";
}
