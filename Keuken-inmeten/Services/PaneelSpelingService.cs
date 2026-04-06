namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class PaneelSpelingService
{
    public const double DefaultRandSpeling = 2.0;
    private const double Tolerantie = 0.6;

    public static double NormaliseerRandSpeling(double waarde)
    {
        if (double.IsNaN(waarde) || double.IsInfinity(waarde))
            return DefaultRandSpeling;

        return Math.Round(Math.Max(0, waarde), 1);
    }

    public static PaneelMaatInfo BerekenMaatInfo(
        PaneelRechthoek openingsRechthoek,
        IEnumerable<PaneelRechthoek> buurRechthoekenBron,
        double randSpelingPerRaakrand)
    {
        var randSpeling = NormaliseerRandSpeling(randSpelingPerRaakrand);
        var buurRechthoeken = buurRechthoekenBron.ToList();

        var raaktLinks = RaaktVerticaleRand(openingsRechthoek.XPositie, openingsRechthoek.HoogteVanVloer, openingsRechthoek.Bovenzijde,
            VerticaleSegmentenVanPanelen(buurRechthoeken));
        var raaktRechts = RaaktVerticaleRand(openingsRechthoek.Rechterkant, openingsRechthoek.HoogteVanVloer, openingsRechthoek.Bovenzijde,
            VerticaleSegmentenVanPanelen(buurRechthoeken));
        var raaktOnder = RaaktHorizontaleRand(openingsRechthoek.HoogteVanVloer, openingsRechthoek.XPositie, openingsRechthoek.Rechterkant,
            HorizontaleSegmentenVanPanelen(buurRechthoeken));
        var raaktBoven = RaaktHorizontaleRand(openingsRechthoek.Bovenzijde, openingsRechthoek.XPositie, openingsRechthoek.Rechterkant,
            HorizontaleSegmentenVanPanelen(buurRechthoeken));

        var linksInset = raaktLinks ? randSpeling : 0;
        var rechtsInset = raaktRechts ? randSpeling : 0;
        var onderInset = raaktOnder ? randSpeling : 0;
        var bovenInset = raaktBoven ? randSpeling : 0;

        var maxHorizontaleInset = Math.Max(0, openingsRechthoek.Breedte - 1);
        var totaleHorizontaleInset = Math.Min(maxHorizontaleInset, linksInset + rechtsInset);
        if (totaleHorizontaleInset < linksInset + rechtsInset)
        {
            linksInset = Math.Min(linksInset, totaleHorizontaleInset / 2.0);
            rechtsInset = Math.Min(rechtsInset, totaleHorizontaleInset - linksInset);
        }

        var maxVerticaleInset = Math.Max(0, openingsRechthoek.Hoogte - 1);
        var totaleVerticaleInset = Math.Min(maxVerticaleInset, onderInset + bovenInset);
        if (totaleVerticaleInset < onderInset + bovenInset)
        {
            onderInset = Math.Min(onderInset, totaleVerticaleInset / 2.0);
            bovenInset = Math.Min(bovenInset, totaleVerticaleInset - onderInset);
        }

        var paneelRechthoek = new PaneelRechthoek
        {
            XPositie = Math.Round(openingsRechthoek.XPositie + linksInset, 1),
            HoogteVanVloer = Math.Round(openingsRechthoek.HoogteVanVloer + onderInset, 1),
            Breedte = Math.Round(Math.Max(1, openingsRechthoek.Breedte - linksInset - rechtsInset), 1),
            Hoogte = Math.Round(Math.Max(1, openingsRechthoek.Hoogte - onderInset - bovenInset), 1)
        };

        return new PaneelMaatInfo
        {
            OpeningsRechthoek = openingsRechthoek.Kopie(),
            PaneelRechthoek = paneelRechthoek,
            RandSpelingPerRaakrand = randSpeling,
            RaaktLinks = raaktLinks,
            RaaktRechts = raaktRechts,
            RaaktOnder = raaktOnder,
            RaaktBoven = raaktBoven
        };
    }

    private static bool RaaktVerticaleRand(double x, double startY, double eindY, IEnumerable<(double x, double start, double eind)> segmenten)
        => segmenten.Any(segment =>
            Math.Abs(segment.x - x) <= Tolerantie &&
            Overlap(segment.start, segment.eind, startY, eindY) > 0.5);

    private static bool RaaktHorizontaleRand(double y, double startX, double eindX, IEnumerable<(double y, double start, double eind)> segmenten)
        => segmenten.Any(segment =>
            Math.Abs(segment.y - y) <= Tolerantie &&
            Overlap(segment.start, segment.eind, startX, eindX) > 0.5);

    private static IEnumerable<(double x, double start, double eind)> VerticaleSegmentenVanPanelen(IEnumerable<PaneelRechthoek> panelen)
    {
        foreach (var paneel in panelen)
        {
            yield return (paneel.XPositie, paneel.HoogteVanVloer, paneel.Bovenzijde);
            yield return (paneel.Rechterkant, paneel.HoogteVanVloer, paneel.Bovenzijde);
        }
    }

    private static IEnumerable<(double y, double start, double eind)> HorizontaleSegmentenVanPanelen(IEnumerable<PaneelRechthoek> panelen)
    {
        foreach (var paneel in panelen)
        {
            yield return (paneel.HoogteVanVloer, paneel.XPositie, paneel.Rechterkant);
            yield return (paneel.Bovenzijde, paneel.XPositie, paneel.Rechterkant);
        }
    }

    private static double Overlap(double startA, double eindA, double startB, double eindB)
        => Math.Min(eindA, eindB) - Math.Max(startA, startB);
}
