namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class WandUitlijnControleService
{
    public const double VerdachteAfwijkingDrempelMm = 25.0;
    private const double MinRelevanteAfwijkingMm = 0.1;
    private const double MinOverlapMm = 1.0;

    public static IReadOnlyList<WandUitlijnWaarschuwing> BepaalWaarschuwingen(
        IEnumerable<KeukenWand> wanden,
        IEnumerable<Kast> kastenBron)
    {
        var kastenOpId = kastenBron.ToDictionary(kast => kast.Id);

        return wanden
            .Select(wand =>
            {
                var wandKasten = wand.KastIds
                    .Select(kastId => kastenOpId.GetValueOrDefault(kastId))
                    .Where(kast => kast is not null)
                    .Cast<Kast>()
                    .ToList();
                var waarschuwingen = BepaalWandWaarschuwingen(wandKasten);

                return new WandUitlijnWaarschuwing(
                    wand.Id,
                    string.IsNullOrWhiteSpace(wand.Naam) ? "Onbekende wand" : wand.Naam,
                    waarschuwingen);
            })
            .Where(wand => wand.Waarschuwingen.Count > 0)
            .ToList();
    }

    private static IReadOnlyList<KastUitlijnWaarschuwing> BepaalWandWaarschuwingen(IReadOnlyList<Kast> kasten)
    {
        var waarschuwingen = new List<KastUitlijnWaarschuwing>();

        for (var i = 0; i < kasten.Count; i++)
        {
            for (var j = i + 1; j < kasten.Count; j++)
            {
                var afwijkingen = BepaalAfwijkingen(kasten[i], kasten[j]);
                if (afwijkingen.Count == 0)
                    continue;

                waarschuwingen.Add(new KastUitlijnWaarschuwing(
                    kasten[i].Naam,
                    kasten[j].Naam,
                    afwijkingen));
            }
        }

        return waarschuwingen
            .OrderBy(waarschuwing => waarschuwing.KleinsteAfwijkingMm)
            .ThenBy(waarschuwing => waarschuwing.EersteKastNaam, StringComparer.OrdinalIgnoreCase)
            .ThenBy(waarschuwing => waarschuwing.TweedeKastNaam, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<UitlijnAfwijking> BepaalAfwijkingen(Kast eerste, Kast tweede)
    {
        var eersteLinks = eerste.XPositie;
        var eersteRechts = eerste.XPositie + eerste.Breedte;
        var eersteOnder = eerste.HoogteVanVloer;
        var eersteBoven = eerste.HoogteVanVloer + eerste.Hoogte;

        var tweedeLinks = tweede.XPositie;
        var tweedeRechts = tweede.XPositie + tweede.Breedte;
        var tweedeOnder = tweede.HoogteVanVloer;
        var tweedeBoven = tweede.HoogteVanVloer + tweede.Hoogte;

        var horizontaleOverlap = Overlap(eersteLinks, eersteRechts, tweedeLinks, tweedeRechts);
        var verticaleOverlap = Overlap(eersteOnder, eersteBoven, tweedeOnder, tweedeBoven);
        var horizontaleAfstand = AfstandTussenSegmenten(eersteLinks, eersteRechts, tweedeLinks, tweedeRechts);

        var afwijkingen = new List<UitlijnAfwijking>();
        VoegAfwijkingToe(
            afwijkingen,
            horizontaleOverlap > MinOverlapMm,
            "verticale aansluiting",
            Math.Min(
                Math.Abs(eersteBoven - tweedeOnder),
                Math.Abs(tweedeBoven - eersteOnder)));
        VoegAfwijkingToe(
            afwijkingen,
            horizontaleAfstand <= MinOverlapMm && verticaleOverlap > MinOverlapMm,
            "onderkanten uit lijn",
            Math.Abs(eersteOnder - tweedeOnder));
        VoegAfwijkingToe(
            afwijkingen,
            horizontaleAfstand <= MinOverlapMm && verticaleOverlap > MinOverlapMm,
            "bovenkanten uit lijn",
            Math.Abs(eersteBoven - tweedeBoven));

        return afwijkingen;
    }

    private static void VoegAfwijkingToe(
        List<UitlijnAfwijking> afwijkingen,
        bool isRelevant,
        string label,
        double afwijkingMm)
    {
        if (!isRelevant)
            return;

        if (afwijkingMm < MinRelevanteAfwijkingMm || afwijkingMm >= VerdachteAfwijkingDrempelMm)
            return;

        afwijkingen.Add(new UitlijnAfwijking(label, Math.Round(afwijkingMm, 1)));
    }

    private static double Overlap(double eersteStart, double eersteEinde, double tweedeStart, double tweedeEinde)
        => Math.Min(eersteEinde, tweedeEinde) - Math.Max(eersteStart, tweedeStart);

    private static double AfstandTussenSegmenten(double eersteStart, double eersteEinde, double tweedeStart, double tweedeEinde)
    {
        if (Overlap(eersteStart, eersteEinde, tweedeStart, tweedeEinde) > MinOverlapMm)
            return 0;

        return Math.Max(eersteStart, tweedeStart) - Math.Min(eersteEinde, tweedeEinde);
    }
}

public sealed record WandUitlijnWaarschuwing(
    Guid WandId,
    string WandNaam,
    IReadOnlyList<KastUitlijnWaarschuwing> Waarschuwingen);

public sealed record KastUitlijnWaarschuwing(
    string EersteKastNaam,
    string TweedeKastNaam,
    IReadOnlyList<UitlijnAfwijking> Afwijkingen)
{
    public double KleinsteAfwijkingMm => Afwijkingen.Count == 0 ? double.MaxValue : Afwijkingen.Min(item => item.AfwijkingMm);
}

public sealed record UitlijnAfwijking(string Label, double AfwijkingMm);
