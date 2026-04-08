namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class PaneelMaatUitlegHelper
{
    public const string MaatInOpeningLabel = "Maat in de opening";
    public const string MaatInOpeningLabelMetTerm = "Maat in de opening (openingsmaat)";
    public const string MaatOmTeBestellenLabel = "Maat om te bestellen";
    public const string MaatOmTeBestellenLabelMetTerm = "Maat om te bestellen (werkmaat)";
    public const string RandSpelingLabel = "Vrije ruimte langs een rakende rand";

    public static string BreedteFormule(PaneelMaatInfo maatInfo)
        => BouwFormule(
            maatInfo.OpeningsRechthoek.Breedte,
            maatInfo.PaneelRechthoek.Breedte,
            SpelingDelen(maatInfo.RaaktLinks, maatInfo.InkortingLinks, "links",
                         maatInfo.RaaktRechts, maatInfo.InkortingRechts, "rechts"));

    public static string HoogteFormule(PaneelMaatInfo maatInfo)
        => BouwFormule(
            maatInfo.OpeningsRechthoek.Hoogte,
            maatInfo.PaneelRechthoek.Hoogte,
            SpelingDelen(maatInfo.RaaktOnder, maatInfo.InkortingOnder, "onder",
                         maatInfo.RaaktBoven, maatInfo.InkortingBoven, "boven"));

    public static string BreedteAftrekUitleg(PaneelMaatInfo maatInfo)
        => BouwAftrekUitleg(
            "Links en rechts",
            SpelingRanden(maatInfo.RaaktLinks, "links", maatInfo.RaaktRechts, "rechts"));

    public static string HoogteAftrekUitleg(PaneelMaatInfo maatInfo)
        => BouwAftrekUitleg(
            "Onder en boven",
            SpelingRanden(maatInfo.RaaktOnder, "onder", maatInfo.RaaktBoven, "boven"));

    private static string BouwFormule(double opening, double paneel, IReadOnlyList<string> aftrekDelen)
        => aftrekDelen.Count == 0
            ? $"{FormatMm(opening)} maat in de opening - geen aftrek = {FormatMm(paneel)} maat om te bestellen"
            : $"{FormatMm(opening)} maat in de opening - {string.Join(" - ", aftrekDelen)} = {FormatMm(paneel)} maat om te bestellen";

    private static string BouwAftrekUitleg(string asLabel, IReadOnlyList<string> randen)
        => randen.Count == 0
            ? $"{asLabel}: geen aftrek, deze randen zijn vrij."
            : $"{asLabel}: aftrek op {SamenvoegenMetEn(randen)} doordat die randen een andere kast-, apparaat- of paneelrand raken.";

    private static IReadOnlyList<string> SpelingDelen(
        bool eersteRaakt,
        double eersteWaarde,
        string eersteLabel,
        bool tweedeRaakt,
        double tweedeWaarde,
        string tweedeLabel)
    {
        var delen = new List<string>();

        if (eersteRaakt)
            delen.Add($"{FormatMm(eersteWaarde)} {eersteLabel}");

        if (tweedeRaakt)
            delen.Add($"{FormatMm(tweedeWaarde)} {tweedeLabel}");

        return delen;
    }

    private static IReadOnlyList<string> SpelingRanden(bool eersteRaakt, string eersteLabel, bool tweedeRaakt, string tweedeLabel)
    {
        var randen = new List<string>();

        if (eersteRaakt)
            randen.Add(eersteLabel);

        if (tweedeRaakt)
            randen.Add(tweedeLabel);

        return randen;
    }

    private static string SamenvoegenMetEn(IReadOnlyList<string> delen)
        => delen.Count switch
        {
            0 => "",
            1 => delen[0],
            2 => $"{delen[0]} en {delen[1]}",
            _ => $"{string.Join(", ", delen.Take(delen.Count - 1))} en {delen[^1]}"
        };

    private static string FormatMm(double waarde) => $"{waarde:0.#} mm";
}
