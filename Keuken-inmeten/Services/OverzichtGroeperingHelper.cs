namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class OverzichtGroeperingHelper
{
    public sealed record Telling(string Label, int Aantal);

    public sealed record PaneelReviewItem(
        int Volgnummer,
        Guid Id,
        string WandNaam,
        string KastenLabel,
        PaneelType Type,
        string TypeLabel,
        double Breedte,
        double Hoogte,
        string PlaatsingLabel,
        string ScharnierLabel);

    public sealed record PaneelReviewGroep(
        string WandNaam,
        int AantalPanelen,
        List<Telling> TypeTellingen,
        List<PaneelReviewItem> Items);

    public sealed record BestellijstWandGroep(
        string WandNaam,
        int Orderregels,
        int PanelenTotaal,
        List<Telling> PaneelTypeTellingen,
        List<BestellijstItem> Items);

    public static List<Telling> MaakTellingen<T>(
        IEnumerable<T> items,
        Func<T, string> labelSelector,
        Func<T, int>? aantalSelector = null)
        => items
            .Select(item => new
            {
                Label = (labelSelector(item) ?? string.Empty).Trim(),
                Aantal = Math.Max(0, aantalSelector?.Invoke(item) ?? 1)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Label))
            .GroupBy(item => item.Label, StringComparer.CurrentCultureIgnoreCase)
            .Select(groep => new Telling(groep.First().Label, groep.Sum(item => item.Aantal)))
            .OrderByDescending(telling => telling.Aantal)
            .ThenBy(telling => telling.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public static string FormatteerTellingen(IEnumerable<Telling> tellingen)
        => string.Join(" · ", tellingen.Select(telling => $"{telling.Label} {telling.Aantal}"));

    public static List<PaneelReviewGroep> GroepeerPaneelToewijzingen(KeukenStateService state)
        => state.Toewijzingen
            .Select((toewijzing, index) =>
            {
                var kasten = state.ZoekKasten(toewijzing.KastIds);
                var kastenLabel = string.Join(" + ", kasten.Select(kast => kast.Naam));
                var wandNaam = state.WandNaamVoorKasten(toewijzing.KastIds, "Onbekende wand");

                return new PaneelReviewItem(
                    index + 1,
                    toewijzing.Id,
                    wandNaam,
                    string.IsNullOrWhiteSpace(kastenLabel) ? "Onbekende kast" : kastenLabel,
                    toewijzing.Type,
                    VisualisatieHelper.PaneelTypeLabel(toewijzing.Type),
                    toewijzing.Breedte,
                    toewijzing.Hoogte,
                    PlaatsingLabel(toewijzing),
                    ScharnierLabel(toewijzing));
            })
            .GroupBy(item => item.WandNaam, StringComparer.CurrentCultureIgnoreCase)
            .Select(groep =>
            {
                var items = groep.ToList();
                return new PaneelReviewGroep(
                    groep.First().WandNaam,
                    items.Count,
                    MaakTellingen(items, item => item.TypeLabel),
                    items);
            })
            .OrderBy(groep => groep.WandNaam, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public static List<BestellijstWandGroep> GroepeerBestellijstOpWand(IEnumerable<BestellijstItem> items)
        => items
            .GroupBy(BepaalBestellijstWandSleutel)
            .Select(groep =>
            {
                var itemLijst = groep.ToList();
                var wandNaam = itemLijst
                    .Select(item => NormaliseerWandNaam(item.WandNaam))
                    .FirstOrDefault(naam => !string.IsNullOrWhiteSpace(naam)) ?? "Onbekende wand";
                return new BestellijstWandGroep(
                    wandNaam,
                    itemLijst.Count,
                    itemLijst.Sum(item => item.Aantal),
                    MaakTellingen(itemLijst, item => item.PaneelRolLabel, item => item.Aantal),
                    itemLijst);
            })
            .OrderBy(groep => groep.WandNaam, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public static string PlaatsingLabel(PaneelToewijzing toewijzing)
        => toewijzing.XPositie is double x && toewijzing.HoogteVanVloer is double y
            ? $"Links {FormatMm(x)} · onderkant {FormatMm(y)}"
            : "Volledige span over de selectie";

    public static string ScharnierLabel(PaneelToewijzing toewijzing)
        => toewijzing.Type == PaneelType.Deur
            ? $"Scharnier {toewijzing.ScharnierZijde.ToString().ToLowerInvariant()} · hart scharnierpot {FormatMm(toewijzing.PotHartVanRand)}"
            : "Geen scharnierdetails";

    private static string NormaliseerWandNaam(string? wandNaam)
        => string.IsNullOrWhiteSpace(wandNaam) ? "Onbekende wand" : wandNaam.Trim();

    private static string BepaalBestellijstWandSleutel(BestellijstItem item)
        => item.WandId?.ToString("N") ?? NormaliseerWandNaam(item.WandNaam).ToUpperInvariant();

    private static string FormatMm(double waarde) => $"{waarde:0.#} mm";
}
