namespace Keuken_inmeten.Services;

using System.Globalization;
using Keuken_inmeten.Models;

/// <summary>
/// Gedeelde hulpmethoden voor SVG-visualisatie en opmaak.
/// Centraliseert kleuren, labels en nummering die in meerdere componenten worden gebruikt.
/// </summary>
public static class VisualisatieHelper
{
    public static string KastKleur(KastType type) => type switch
    {
        KastType.Onderkast => "#f5deb3",
        KastType.Bovenkast => "#d4e6f1",
        KastType.HogeKast  => "#d5f5e3",
        _ => "#f5deb3"
    };

    public static string ApparaatKleur(ApparaatType type) => type switch
    {
        ApparaatType.Oven       => "#e8d5c4",
        ApparaatType.Magnetron  => "#d4d4e8",
        ApparaatType.Vaatwasser => "#c4d8e8",
        ApparaatType.Koelkast   => "#c4e8e8",
        ApparaatType.Vriezer    => "#c4d4f0",
        ApparaatType.Kookplaat  => "#e8d4c4",
        ApparaatType.Afzuigkap  => "#d8d8d8",
        _ => "#d8d8d8"
    };

    public static string PaneelTypeLabel(PaneelType type) => type switch
    {
        PaneelType.Deur        => "Deur",
        PaneelType.LadeFront   => "Ladefront",
        PaneelType.BlindPaneel => "Blind paneel",
        _ => type.ToString()
    };

    /// <summary>Format a double for SVG attributes using invariant culture (1 decimal).</summary>
    public static string Fmt(double v) => v.ToString("F1", CultureInfo.InvariantCulture);

    /// <summary>Format a double for data output with up to 3 decimal places.</summary>
    public static string FmtData(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
