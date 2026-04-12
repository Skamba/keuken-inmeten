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

    public static string FormatZaagmaat(double breedteMm, double hoogteMm)
        => $"{breedteMm:0.#} × {hoogteMm:0.#} mm";

    public static string FormatVierkanteMeter(double value) => $"{value:0.###} m²";

    public static string FormatRegelCode(int regelNummer) => $"R{regelNummer:00}";

    public static string FormatBoorbeeldSamenvatting(string scharnierLabel, int aantalBoorgaten)
    {
        if (aantalBoorgaten <= 0)
            return "Geen 35 mm potscharniergaten";

        var zijdeTekst = string.IsNullOrWhiteSpace(scharnierLabel) || scharnierLabel == "—"
            ? "Scharnierzijde n.v.t."
            : $"Scharnier {scharnierLabel.ToLowerInvariant()}";
        var gatenTekst = $"{aantalBoorgaten} {(aantalBoorgaten == 1 ? "potscharniergat" : "potscharniergaten")}";
        return $"{zijdeTekst} · {gatenTekst}";
    }

    public static string FormatBronLocaties(IReadOnlyList<string> bronLocaties)
        => bronLocaties.Count == 0 ? "—" : string.Join(" | ", bronLocaties);
}
