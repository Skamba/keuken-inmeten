namespace Keuken_inmeten.Services;

public static class StappenFlowHelper
{
    private static readonly AppStap[] Stappen =
    [
        new("kasten", "kasten", 1, "Indeling", "Bouw uw keuken op per wand en voeg kasten toe."),
        new("panelen", "panelen", 2, "Panelen", "Wijs panelen toe en bepaal maat, plaatsing en scharnierzijde."),
        new("verificatie", "verificatie", 3, "Verificatie", "Controleer openingen, paneelmaten en boorgaten."),
        new("bestellijst", "bestellijst", 4, "Bestellijst", "Bekijk orderregels en exporteer de lijst."),
        new("zaagplan", "zaagplan", 5, "Zaagplan", "Verdeel panelen over standaard platen.")
    ];

    public static IReadOnlyList<AppStap> AlleStappen => Stappen;

    public static AppStap Stap(string id)
        => Stappen.First(stap => stap.Id == id);

    public static StappenFlowStatus BepaalStatus(KeukenStateService state)
        => new(
            HeeftKasten: state.Kasten.Count > 0,
            HeeftPanelen: state.BerekenResultaten().Count > 0);

    public static AppStap BepaalVervolgStap(StappenFlowStatus status)
        => !status.HeeftKasten
            ? Stap("kasten")
            : !status.HeeftPanelen
                ? Stap("panelen")
                : Stap("verificatie");

    public static bool IsBeschikbaar(string stapId, StappenFlowStatus status)
        => BepaalVereisteStap(stapId, status) is null;

    public static bool MagNavigeren(string targetStapId, string huidigeStapId, StappenFlowStatus status)
        => Stap(targetStapId).Nummer <= Stap(huidigeStapId).Nummer || IsBeschikbaar(targetStapId, status);

    public static StapRouteGate? BepaalRouteGate(string stapId, StappenFlowStatus status)
    {
        var huidigeStap = Stap(stapId);
        var vereisteStap = BepaalVereisteStap(stapId, status);
        if (vereisteStap is null)
            return null;

        var reden = vereisteStap.Id switch
        {
            "kasten" => $"Voeg eerst ten minste één kast toe in stap {vereisteStap.Nummer}: {vereisteStap.Label} voordat u naar stap {huidigeStap.Nummer}: {huidigeStap.Label} gaat.",
            "panelen" => $"Wijs eerst panelen toe in stap {vereisteStap.Nummer}: {vereisteStap.Label} voordat u naar stap {huidigeStap.Nummer}: {huidigeStap.Label} gaat.",
            _ => $"Rond eerst stap {vereisteStap.Nummer}: {vereisteStap.Label} af voordat u deze stap opent."
        };

        return new StapRouteGate(
            huidigeStap,
            vereisteStap,
            reden,
            MaakGaNaarLabel(vereisteStap));
    }

    public static string MaakGaNaarLabel(AppStap stap)
        => stap.Id == "kasten"
            ? "Ga naar indeling"
            : $"Ga naar {stap.Label.ToLowerInvariant()}";

    public static string MaakTerugNaarLabel(string stapId)
        => $"Terug naar {Stap(stapId).Label.ToLowerInvariant()}";

    public static string MaakVolgendeLabel(string stapId)
        => Stap(stapId).Id == "kasten"
            ? "Begin met indeling"
            : $"Ga naar {Stap(stapId).Label.ToLowerInvariant()}";

    private static AppStap? BepaalVereisteStap(string stapId, StappenFlowStatus status) => stapId switch
    {
        "panelen" when !status.HeeftKasten => Stap("kasten"),
        "verificatie" or "bestellijst" or "zaagplan" when !status.HeeftPanelen
            => status.HeeftKasten ? Stap("panelen") : Stap("kasten"),
        _ => null
    };
}

public sealed record AppStap(string Id, string Route, int Nummer, string Label, string Beschrijving);

public readonly record struct StappenFlowStatus(bool HeeftKasten, bool HeeftPanelen);

public sealed record StapRouteGate(AppStap HuidigeStap, AppStap VereisteStap, string Reden, string ActieLabel);
