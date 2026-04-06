namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class ApparaatLayoutService
{
    private const double RasterMm = 10.0;

    public static (double xPositie, double hoogteVanVloer) BepaalStandaardPlaatsing(
        KeukenWand wand,
        Apparaat apparaat,
        IEnumerable<Kast> kastenBron,
        IEnumerable<Apparaat> apparatenBron)
    {
        var kasten = kastenBron.ToList();
        var apparaten = apparatenBron.ToList();
        var maxX = Math.Max(0, wand.Breedte - apparaat.Breedte);
        var maxY = Math.Max(0, wand.Hoogte - apparaat.Hoogte);
        var voorkeursY = ClampOpRaster(Math.Clamp(wand.PlintHoogte, 0, maxY));

        if (TryVindVrijePlaats(voorkeursY, maxX, maxY, apparaat, kasten, apparaten, out var plaatsing))
            return plaatsing;

        for (double y = 0; y <= maxY + 0.001; y += RasterMm)
        {
            var kandidaatY = ClampOpRaster(y);
            if (Math.Abs(kandidaatY - voorkeursY) < 0.001)
                continue;

            if (TryVindVrijePlaats(kandidaatY, maxX, maxY, apparaat, kasten, apparaten, out plaatsing))
                return plaatsing;
        }

        return (0, voorkeursY);
    }

    public static bool HeeftOverlap(Apparaat apparaat, Kast kast)
        => HeeftOverlap(
            apparaat.XPositie,
            apparaat.HoogteVanVloer,
            apparaat.Breedte,
            apparaat.Hoogte,
            kast.XPositie,
            kast.HoogteVanVloer,
            kast.Breedte,
            kast.Hoogte);

    public static bool HeeftOverlap(Apparaat links, Apparaat rechts)
        => HeeftOverlap(
            links.XPositie,
            links.HoogteVanVloer,
            links.Breedte,
            links.Hoogte,
            rechts.XPositie,
            rechts.HoogteVanVloer,
            rechts.Breedte,
            rechts.Hoogte);

    private static bool TryVindVrijePlaats(
        double y,
        double maxX,
        double maxY,
        Apparaat apparaat,
        IReadOnlyList<Kast> kasten,
        IReadOnlyList<Apparaat> apparaten,
        out (double xPositie, double hoogteVanVloer) plaatsing)
    {
        y = Math.Clamp(ClampOpRaster(y), 0, maxY);

        for (double x = 0; x <= maxX + 0.001; x += RasterMm)
        {
            var kandidaatX = Math.Clamp(ClampOpRaster(x), 0, maxX);
            if (IsVrijePlaats(kandidaatX, y, apparaat, kasten, apparaten))
            {
                plaatsing = (kandidaatX, y);
                return true;
            }
        }

        plaatsing = default;
        return false;
    }

    private static bool IsVrijePlaats(
        double x,
        double y,
        Apparaat apparaat,
        IReadOnlyList<Kast> kasten,
        IReadOnlyList<Apparaat> apparaten)
    {
        foreach (var kast in kasten)
        {
            if (HeeftOverlap(x, y, apparaat.Breedte, apparaat.Hoogte, kast.XPositie, kast.HoogteVanVloer, kast.Breedte, kast.Hoogte))
                return false;
        }

        foreach (var bestaandApparaat in apparaten)
        {
            if (HeeftOverlap(x, y, apparaat.Breedte, apparaat.Hoogte, bestaandApparaat.XPositie, bestaandApparaat.HoogteVanVloer, bestaandApparaat.Breedte, bestaandApparaat.Hoogte))
                return false;
        }

        return true;
    }

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

    private static double ClampOpRaster(double waarde) => Math.Round(waarde / RasterMm) * RasterMm;
}
