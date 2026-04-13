namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class KastPlaatsingService
{
    public static bool TryVindVrijePlaatsing(
        KeukenWand wand,
        IEnumerable<Kast> bestaandeKastenBron,
        Kast kast,
        out KastPlaatsing plaatsing,
        Guid? uitsluitenKastId = null)
    {
        var bestaandeKasten = FilterBestaandeKasten(bestaandeKastenBron, uitsluitenKastId);
        if (!TryBepaalPlaatsingsBereik(wand, kast, out var bereik))
        {
            plaatsing = default;
            return false;
        }

        foreach (var kandidaatY in BepaalKastHoogteKandidaten(kast, wand, bestaandeKasten, bereik.MaxY))
        {
            foreach (var kandidaatX in BepaalKastPlaatsKandidaten(kast, bestaandeKasten, bereik.MaxX))
            {
                if (!IsVrijeKastPlaats(kandidaatX, kandidaatY, kast, bestaandeKasten))
                    continue;

                plaatsing = new(Math.Round(kandidaatX, 1), Math.Round(kandidaatY, 1));
                return true;
            }
        }

        plaatsing = default;
        return false;
    }

    public static bool IsVrijePlaatsing(
        KeukenWand wand,
        IEnumerable<Kast> bestaandeKastenBron,
        Kast kast,
        double xPositie,
        double hoogteVanVloer,
        Guid? uitsluitenKastId = null)
    {
        var bestaandeKasten = FilterBestaandeKasten(bestaandeKastenBron, uitsluitenKastId);
        if (!TryBepaalPlaatsingsBereik(wand, kast, out var bereik))
            return false;

        var kandidaat = NormaliseerPlaatsing(xPositie, hoogteVanVloer);
        return PastBinnenBereik(bereik, kandidaat)
            && IsVrijeKastPlaats(kandidaat.XPositie, kandidaat.HoogteVanVloer, kast, bestaandeKasten);
    }

    private static List<Kast> FilterBestaandeKasten(IEnumerable<Kast> bestaandeKastenBron, Guid? uitsluitenKastId)
        => bestaandeKastenBron
            .Where(bestaandeKast => bestaandeKast.Id != uitsluitenKastId)
            .ToList();

    private static bool TryBepaalPlaatsingsBereik(KeukenWand wand, Kast kast, out PlaatsingsBereik bereik)
    {
        var maxX = wand.Breedte - kast.Breedte;
        var maxY = wand.Hoogte - kast.Hoogte;
        if (maxX < -0.001 || maxY < -0.001)
        {
            bereik = default;
            return false;
        }

        bereik = new(Math.Max(0, maxX), Math.Max(0, maxY));
        return true;
    }

    private static KastPlaatsing NormaliseerPlaatsing(double xPositie, double hoogteVanVloer)
        => new(
            KeukenDomeinValidatieService.NormaliseerPositie(xPositie),
            KeukenDomeinValidatieService.NormaliseerPositie(hoogteVanVloer));

    private static bool PastBinnenBereik(PlaatsingsBereik bereik, KastPlaatsing plaatsing)
        => plaatsing.XPositie <= bereik.MaxX + 0.001
            && plaatsing.HoogteVanVloer <= bereik.MaxY + 0.001;

    private static IEnumerable<double> BepaalKastPlaatsKandidaten(Kast kast, IReadOnlyList<Kast> bestaandeKasten, double maxX)
    {
        var gezien = new HashSet<double>();

        if (VoegKandidaatToe(Math.Clamp(KeukenDomeinValidatieService.NormaliseerPositie(kast.XPositie), 0, Math.Max(0, maxX))))
            yield return Math.Clamp(KeukenDomeinValidatieService.NormaliseerPositie(kast.XPositie), 0, Math.Max(0, maxX));

        if (VoegKandidaatToe(0))
            yield return 0;

        foreach (var kandidaat in bestaandeKasten
                     .OrderBy(bestaandeKast => bestaandeKast.XPositie)
                     .Select(bestaandeKast => Math.Clamp(Math.Round(bestaandeKast.XPositie + bestaandeKast.Breedte, 1), 0, Math.Max(0, maxX))))
        {
            if (VoegKandidaatToe(kandidaat))
                yield return kandidaat;
        }

        bool VoegKandidaatToe(double kandidaat)
            => gezien.Add(Math.Round(kandidaat, 1));
    }

    private static IEnumerable<double> BepaalKastHoogteKandidaten(Kast kast, KeukenWand wand, IReadOnlyList<Kast> bestaandeKasten, double maxY)
    {
        var gezien = new HashSet<double>();

        if (VoegKandidaatToe(Math.Clamp(KeukenDomeinValidatieService.NormaliseerPositie(kast.HoogteVanVloer), 0, maxY)))
            yield return Math.Clamp(KeukenDomeinValidatieService.NormaliseerPositie(kast.HoogteVanVloer), 0, maxY);

        var plintHoogte = Math.Clamp(KeukenDomeinValidatieService.NormaliseerPositie(wand.PlintHoogte), 0, maxY);
        if (VoegKandidaatToe(plintHoogte))
            yield return plintHoogte;

        if (VoegKandidaatToe(0))
            yield return 0;

        foreach (var kandidaat in bestaandeKasten
                     .OrderBy(bestaandeKast => bestaandeKast.HoogteVanVloer)
                     .ThenBy(bestaandeKast => bestaandeKast.XPositie)
                     .SelectMany(bestaandeKast => new[]
                     {
                         bestaandeKast.HoogteVanVloer,
                         bestaandeKast.HoogteVanVloer + bestaandeKast.Hoogte,
                         bestaandeKast.HoogteVanVloer - kast.Hoogte
                     })
                     .Select(kandidaat => Math.Clamp(Math.Round(kandidaat, 1), 0, maxY)))
        {
            if (VoegKandidaatToe(kandidaat))
                yield return kandidaat;
        }

        if (VoegKandidaatToe(maxY))
            yield return maxY;

        bool VoegKandidaatToe(double kandidaat)
            => gezien.Add(Math.Round(kandidaat, 1));
    }

    private static bool IsVrijeKastPlaats(double xPositie, double hoogteVanVloer, Kast kast, IReadOnlyList<Kast> bestaandeKasten)
        => bestaandeKasten.All(bestaandeKast =>
            !HeeftOverlap(
                xPositie,
                hoogteVanVloer,
                kast.Breedte,
                kast.Hoogte,
                bestaandeKast.XPositie,
                bestaandeKast.HoogteVanVloer,
                bestaandeKast.Breedte,
                bestaandeKast.Hoogte));

    private static bool HeeftOverlap(
        double linksX,
        double linksY,
        double linksBreedte,
        double linksHoogte,
        double rechtsX,
        double rechtsY,
        double rechtsBreedte,
        double rechtsHoogte)
    {
        var overlapX = Math.Min(linksX + linksBreedte, rechtsX + rechtsBreedte) - Math.Max(linksX, rechtsX);
        var overlapY = Math.Min(linksY + linksHoogte, rechtsY + rechtsHoogte) - Math.Max(linksY, rechtsY);
        return overlapX > 0.1 && overlapY > 0.1;
    }

    private readonly record struct PlaatsingsBereik(double MaxX, double MaxY);
}

public readonly record struct KastPlaatsing(double XPositie, double HoogteVanVloer);
