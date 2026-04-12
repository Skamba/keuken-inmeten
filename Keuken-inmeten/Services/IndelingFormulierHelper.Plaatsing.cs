namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class IndelingFormulierHelper
{
    public static bool TryVindVrijeKastPlaatsing(
        KeukenWand wand,
        IEnumerable<Kast> bestaandeKastenBron,
        Kast kast,
        out (double xPositie, double hoogteVanVloer) plaatsing,
        Guid? uitsluitenKastId = null)
    {
        var bestaandeKasten = bestaandeKastenBron
            .Where(bestaandeKast => bestaandeKast.Id != uitsluitenKastId)
            .ToList();
        var maxX = wand.Breedte - kast.Breedte;
        var maxY = wand.Hoogte - kast.Hoogte;

        if (maxX < -0.001 || maxY < -0.001)
        {
            plaatsing = default;
            return false;
        }

        foreach (var kandidaatY in BepaalKastHoogteKandidaten(kast, wand, bestaandeKasten, Math.Max(0, maxY)))
        {
            foreach (var kandidaatX in BepaalKastPlaatsKandidaten(kast, bestaandeKasten, Math.Max(0, maxX)))
            {
                if (!IsVrijeKastPlaats(kandidaatX, kandidaatY, kast, bestaandeKasten))
                    continue;

                plaatsing = (Math.Round(kandidaatX, 1), Math.Round(kandidaatY, 1));
                return true;
            }
        }

        plaatsing = default;
        return false;
    }

    public static bool IsVrijeKastPlaatsing(
        KeukenWand wand,
        IEnumerable<Kast> bestaandeKastenBron,
        Kast kast,
        double xPositie,
        double hoogteVanVloer,
        Guid? uitsluitenKastId = null)
    {
        var bestaandeKasten = bestaandeKastenBron
            .Where(bestaandeKast => bestaandeKast.Id != uitsluitenKastId)
            .ToList();
        var maxX = wand.Breedte - kast.Breedte;
        var maxY = wand.Hoogte - kast.Hoogte;
        if (maxX < -0.001 || maxY < -0.001)
            return false;

        var kandidaatX = KeukenDomeinValidatieService.NormaliseerPositie(xPositie);
        var kandidaatY = KeukenDomeinValidatieService.NormaliseerPositie(hoogteVanVloer);
        if (kandidaatX > Math.Max(0, maxX) + 0.001 || kandidaatY > Math.Max(0, maxY) + 0.001)
            return false;

        return IsVrijeKastPlaats(kandidaatX, kandidaatY, kast, bestaandeKasten);
    }

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
}
