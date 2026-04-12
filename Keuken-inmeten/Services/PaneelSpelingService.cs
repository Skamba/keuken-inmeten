namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class PaneelSpelingService
{
    public const double DefaultRandSpeling = KeukenDomeinDefaults.ProjectDefaults.PaneelRandSpeling;
    public const double LegacyDefaultRandSpeling = 2.0;
    private const double Tolerantie = 0.6;

    public static double NormaliseerRandSpeling(double waarde)
    {
        if (double.IsNaN(waarde) || double.IsInfinity(waarde))
            return DefaultRandSpeling;

        return Math.Round(Math.Max(0, waarde), 0, MidpointRounding.AwayFromZero);
    }

    public static double NormaliseerLegacyRandSpeling(double waarde)
    {
        if (double.IsNaN(waarde) || double.IsInfinity(waarde))
            return LegacyDefaultRandSpeling;

        return Math.Round(Math.Max(0, waarde), 1);
    }

    public static double MigreerLegacyRandSpeling(double legacyWaarde, bool heeftBestaandePanelen)
    {
        var legacySpeling = NormaliseerLegacyRandSpeling(legacyWaarde);
        if (!heeftBestaandePanelen && Math.Abs(legacySpeling - LegacyDefaultRandSpeling) < 0.001)
            return DefaultRandSpeling;

        return NormaliseerRandSpeling(legacySpeling * 2);
    }

    public static PaneelMaatInfo BerekenMaatInfo(
        PaneelRechthoek openingsRechthoek,
        IEnumerable<PaneelRechthoek> starreBuurRechthoekenBron,
        double totaleRandSpeling)
        => BerekenMaatInfo(openingsRechthoek, starreBuurRechthoekenBron, [], totaleRandSpeling);

    public static PaneelMaatInfo BerekenMaatInfo(
        PaneelRechthoek openingsRechthoek,
        IEnumerable<PaneelRechthoek> starreBuurRechthoekenBron,
        IEnumerable<PaneelRechthoek> buurPanelenBron,
        double totaleRandSpeling)
    {
        var randSpeling = NormaliseerRandSpeling(totaleRandSpeling);
        var starreBuurRechthoeken = starreBuurRechthoekenBron.ToList();
        var buurPanelen = buurPanelenBron.ToList();

        var raaktLinksRigide = RaaktVerticaleRand(
            openingsRechthoek.XPositie,
            openingsRechthoek.HoogteVanVloer,
            openingsRechthoek.Bovenzijde,
            RechterRanden(starreBuurRechthoeken));
        var raaktRechtsRigide = RaaktVerticaleRand(
            openingsRechthoek.Rechterkant,
            openingsRechthoek.HoogteVanVloer,
            openingsRechthoek.Bovenzijde,
            LinkerRanden(starreBuurRechthoeken));
        var raaktOnderRigide = RaaktHorizontaleRand(
            openingsRechthoek.HoogteVanVloer,
            openingsRechthoek.XPositie,
            openingsRechthoek.Rechterkant,
            BovenRanden(starreBuurRechthoeken));
        var raaktBovenRigide = RaaktHorizontaleRand(
            openingsRechthoek.Bovenzijde,
            openingsRechthoek.XPositie,
            openingsRechthoek.Rechterkant,
            OnderRanden(starreBuurRechthoeken));

        var raaktLinksPaneel = RaaktVerticaleRand(
            openingsRechthoek.XPositie,
            openingsRechthoek.HoogteVanVloer,
            openingsRechthoek.Bovenzijde,
            RechterRanden(buurPanelen));
        var raaktRechtsPaneel = RaaktVerticaleRand(
            openingsRechthoek.Rechterkant,
            openingsRechthoek.HoogteVanVloer,
            openingsRechthoek.Bovenzijde,
            LinkerRanden(buurPanelen));
        var raaktOnderPaneel = RaaktHorizontaleRand(
            openingsRechthoek.HoogteVanVloer,
            openingsRechthoek.XPositie,
            openingsRechthoek.Rechterkant,
            BovenRanden(buurPanelen));
        var raaktBovenPaneel = RaaktHorizontaleRand(
            openingsRechthoek.Bovenzijde,
            openingsRechthoek.XPositie,
            openingsRechthoek.Rechterkant,
            OnderRanden(buurPanelen));

        var raaktLinks = raaktLinksRigide || raaktLinksPaneel;
        var raaktRechts = raaktRechtsRigide || raaktRechtsPaneel;
        var raaktOnder = raaktOnderRigide || raaktOnderPaneel;
        var raaktBoven = raaktBovenRigide || raaktBovenPaneel;

        var linksInset = BepaalInset(raaktLinksRigide, raaktLinksPaneel, randSpeling, krijgtGrotePaneelhelft: true);
        var rechtsInset = BepaalInset(raaktRechtsRigide, raaktRechtsPaneel, randSpeling, krijgtGrotePaneelhelft: false);
        var onderInset = BepaalInset(raaktOnderRigide, raaktOnderPaneel, randSpeling, krijgtGrotePaneelhelft: true);
        var bovenInset = BepaalInset(raaktBovenRigide, raaktBovenPaneel, randSpeling, krijgtGrotePaneelhelft: false);

        (linksInset, rechtsInset) = BeperkInsets(linksInset, rechtsInset, openingsRechthoek.Breedte);
        (onderInset, bovenInset) = BeperkInsets(onderInset, bovenInset, openingsRechthoek.Hoogte);

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
            TotaleRandSpeling = randSpeling,
            RaaktLinks = raaktLinks,
            RaaktRechts = raaktRechts,
            RaaktOnder = raaktOnder,
            RaaktBoven = raaktBoven,
            InkortingLinks = linksInset,
            InkortingRechts = rechtsInset,
            InkortingOnder = onderInset,
            InkortingBoven = bovenInset
        };
    }

    private static double BepaalInset(bool raaktRigideRand, bool raaktPaneelRand, double totaleRandSpeling, bool krijgtGrotePaneelhelft)
    {
        if (raaktRigideRand)
            return totaleRandSpeling;

        if (!raaktPaneelRand)
            return 0;

        return krijgtGrotePaneelhelft
            ? Math.Ceiling(totaleRandSpeling / 2.0)
            : Math.Floor(totaleRandSpeling / 2.0);
    }

    private static (double eersteInset, double tweedeInset) BeperkInsets(double eersteInset, double tweedeInset, double beschikbareMaat)
    {
        var maxTotaleInset = Math.Max(0, beschikbareMaat - 1);
        var gewensteInset = eersteInset + tweedeInset;
        if (gewensteInset <= maxTotaleInset + 0.001)
            return (eersteInset, tweedeInset);

        var totaleInset = Math.Min(maxTotaleInset, gewensteInset);
        var beperktEerste = Math.Min(eersteInset, totaleInset / 2.0);
        var beperktTweede = Math.Min(tweedeInset, totaleInset - beperktEerste);
        return (beperktEerste, beperktTweede);
    }

    private static bool RaaktVerticaleRand(double x, double startY, double eindY, IEnumerable<(double x, double start, double eind)> segmenten)
        => segmenten.Any(segment =>
            Math.Abs(segment.x - x) <= Tolerantie &&
            Overlap(segment.start, segment.eind, startY, eindY) > 0.5);

    private static bool RaaktHorizontaleRand(double y, double startX, double eindX, IEnumerable<(double y, double start, double eind)> segmenten)
        => segmenten.Any(segment =>
            Math.Abs(segment.y - y) <= Tolerantie &&
            Overlap(segment.start, segment.eind, startX, eindX) > 0.5);

    private static IEnumerable<(double x, double start, double eind)> LinkerRanden(IEnumerable<PaneelRechthoek> panelen)
    {
        foreach (var paneel in panelen)
            yield return (paneel.XPositie, paneel.HoogteVanVloer, paneel.Bovenzijde);
    }

    private static IEnumerable<(double x, double start, double eind)> RechterRanden(IEnumerable<PaneelRechthoek> panelen)
    {
        foreach (var paneel in panelen)
            yield return (paneel.Rechterkant, paneel.HoogteVanVloer, paneel.Bovenzijde);
    }

    private static IEnumerable<(double y, double start, double eind)> OnderRanden(IEnumerable<PaneelRechthoek> panelen)
    {
        foreach (var paneel in panelen)
            yield return (paneel.HoogteVanVloer, paneel.XPositie, paneel.Rechterkant);
    }

    private static IEnumerable<(double y, double start, double eind)> BovenRanden(IEnumerable<PaneelRechthoek> panelen)
    {
        foreach (var paneel in panelen)
            yield return (paneel.Bovenzijde, paneel.XPositie, paneel.Rechterkant);
    }

    private static double Overlap(double startA, double eindA, double startB, double eindB)
        => Math.Min(eindA, eindB) - Math.Max(startA, startB);
}
