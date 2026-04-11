namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class PaneelConfiguratieHelper
{
    public static IReadOnlyList<PaneelFlowStap> PaneelFlowStappen { get; } =
    [
        new("wand", "1. Kies wand", "Open precies één wand als actieve werkruimte."),
        new("selecteren", "2. Selecteer kast(en)", "Kies een of meer kasten binnen die actieve wand."),
        new("plaatsen", "3. Plaats of pas paneel aan", "Sleep het paneel of kies een vrij vak in het schema."),
        new("opslaan", "4. Controleer en sla op", "Check maat en type en bewaar daarna het paneel.")
    ];

    public static bool KanPaneelOpslaan(PaneelFlowContext context)
        => context.HeeftWandContext
            && context.HeeftSelectie
            && context.HeeftConceptPaneel
            && context.HeeftGeldigeMaat
            && context.RaaktGeselecteerdeKast
            && !context.HeeftConflicterendPaneel;

    public static string BepaalVolgendePaneelStapTekst(PaneelFlowContext context)
        => !context.HeeftWandContext
            ? "Open eerst een wand om de paneel-editor te starten."
            : !context.HeeftSelectie
                ? "Selecteer eerst een of meer kasten binnen de actieve wand."
                : !context.HeeftConceptPaneel
                    ? "Plaats of pas het paneel aan in het schema."
                    : context.HeeftConflicterendPaneel
                        ? "Verplaats of verklein het paneel totdat het geen bestaand paneel meer overlapt."
                    : KanPaneelOpslaan(context)
                        ? "Sla het paneel op zodra maat en type kloppen."
                        : "Controleer maat, type en plaatsing zodat het paneel weer een kast raakt.";

    public static string BepaalOpslaanStatusTekst(PaneelFlowContext context)
    {
        if (!context.HeeftWandContext)
            return "Nee, er is nog geen actieve wand.";

        if (!context.HeeftSelectie)
            return "Nee, er is nog geen kastselectie.";

        if (!context.HeeftConceptPaneel)
            return "Nee, er is nog geen paneelpositie.";

        if (!context.HeeftGeldigeMaat)
            return "Nee, breedte en hoogte moeten groter dan 0 zijn.";

        if (context.HeeftConflicterendPaneel)
            return "Nee, het paneel overlapt nog een bestaand paneel.";

        return context.RaaktGeselecteerdeKast
            ? "Ja, u kunt nu opslaan."
            : "Nee, het paneel moet minimaal een geselecteerde kast raken.";
    }

    public static string BepaalGeselecteerdePaneelStatusTekst(PaneelFlowContext context)
    {
        if (!context.HeeftWandContext)
            return "Nog geen wand geopend.";

        return !context.HeeftSelectie
            ? $"{context.ActieveWandNaam} · nog geen kasten geselecteerd."
            : $"{context.ActieveWandNaam} · {context.GeselecteerdeKastNamen}";
    }

    public static string BepaalPaneelFlowStatus(string stapId, PaneelFlowContext context)
    {
        return stapId switch
        {
            "wand" => context.HeeftWandContext ? "done" : "active",
            "selecteren" => !context.HeeftWandContext ? "todo" : context.HeeftSelectie ? "done" : "active",
            "plaatsen" => !context.HeeftWandContext || !context.HeeftSelectie ? "todo" : context.HeeftConceptPaneel ? "done" : "active",
            "opslaan" => !context.HeeftWandContext || !context.HeeftSelectie || !context.HeeftConceptPaneel ? "todo" : "active",
            _ => "todo"
        };
    }

    public static string PaneelFlowContainerClass(string status)
        => status switch
        {
            "done" => "border-success bg-success bg-opacity-10",
            "active" => "border-primary bg-primary bg-opacity-10",
            _ => "bg-light"
        };

    public static string PaneelFlowBadgeClass(string status)
        => status switch
        {
            "done" => "bg-success",
            "active" => "bg-primary",
            _ => "bg-secondary"
        };

    public static string PaneelFlowLabel(string status)
        => status switch
        {
            "done" => "Klaar",
            "active" => "Nu",
            _ => "Wacht"
        };

    public static PaneelRechthoek SnapPaneel(
        string bewerking,
        PaneelRechthoek voorstel,
        PaneelRechthoek selectieBereik,
        IEnumerable<double> xTargets,
        IEnumerable<double> yTargets,
        double snapDrempel = 24.0)
    {
        var left = voorstel.XPositie;
        var right = voorstel.Rechterkant;
        var bottom = voorstel.HoogteVanVloer;
        var top = voorstel.Bovenzijde;
        var xTargetLijst = xTargets.Distinct().ToList();
        var yTargetLijst = yTargets.Distinct().ToList();

        switch (bewerking)
        {
            case "move":
                var nieuweX = SnapPositieMetBehoudVanMaat(voorstel.XPositie, voorstel.Breedte, xTargetLijst, snapDrempel);
                var nieuweY = SnapPositieMetBehoudVanMaat(voorstel.HoogteVanVloer, voorstel.Hoogte, yTargetLijst, snapDrempel);
                return PaneelLayoutService.ClampBinnen(new PaneelRechthoek
                {
                    XPositie = nieuweX,
                    HoogteVanVloer = nieuweY,
                    Breedte = voorstel.Breedte,
                    Hoogte = voorstel.Hoogte
                }, selectieBereik);

            case "nw":
                left = SnapEdge(left, xTargetLijst, snapDrempel);
                top = SnapEdge(top, yTargetLijst, snapDrempel);
                break;

            case "ne":
                right = SnapEdge(right, xTargetLijst, snapDrempel);
                top = SnapEdge(top, yTargetLijst, snapDrempel);
                break;

            case "sw":
                left = SnapEdge(left, xTargetLijst, snapDrempel);
                bottom = SnapEdge(bottom, yTargetLijst, snapDrempel);
                break;

            case "se":
            default:
                right = SnapEdge(right, xTargetLijst, snapDrempel);
                top = SnapEdge(top, yTargetLijst, snapDrempel);
                break;
        }

        return BouwRechthoek(left, right, bottom, top, selectieBereik);
    }

    public static List<PaneelRechthoek> BepaalVrijeSegmenten(
        PaneelRechthoek selectieBereik,
        IEnumerable<PaneelRechthoek> bestaandePaneelRechthoeken)
    {
        var bezetteSegmenten = bestaandePaneelRechthoeken
            .Where(paneel => HeeftHorizontaleOverlap(paneel, selectieBereik))
            .ToList();
        if (bezetteSegmenten.Count == 0)
            return [];

        return PaneelLayoutService.BepaalVrijeVerticaleSegmenten(selectieBereik, bezetteSegmenten)
            .OrderBy(segment => segment.HoogteVanVloer)
            .ToList();
    }

    public static PaneelRechthoek BepaalStartRechthoek(PaneelRechthoek selectieBereik, IEnumerable<PaneelRechthoek> vrijeSegmenten)
    {
        var segmenten = vrijeSegmenten.ToList();
        if (segmenten.Count == 0)
            return selectieBereik.Kopie();

        return segmenten
            .OrderByDescending(segment => segment.Hoogte)
            .ThenBy(segment => segment.HoogteVanVloer)
            .First()
            .Kopie();
    }

    public static PaneelRechthoek? BepaalOpdeelBereik(
        PaneelRechthoek selectieBereik,
        IEnumerable<PaneelRechthoek> bestaandePaneelRechthoeken)
    {
        var bezetteSegmenten = bestaandePaneelRechthoeken
            .Where(paneel => HeeftHorizontaleOverlap(paneel, selectieBereik))
            .OrderBy(segment => segment.HoogteVanVloer)
            .ToList();
        if (bezetteSegmenten.Count == 0)
            return selectieBereik.Kopie();

        var vrijeSegmenten = PaneelLayoutService.BepaalVrijeVerticaleSegmenten(selectieBereik, bezetteSegmenten)
            .OrderBy(segment => segment.HoogteVanVloer)
            .ToList();

        return vrijeSegmenten.Count == 1 ? vrijeSegmenten[0].Kopie() : null;
    }

    public static PaneelOpdeelAnalyse AnalyseerOpdeelHoogtes(
        double beschikbareHoogte,
        IEnumerable<double> deelHoogtes,
        double minimumDeelHoogte = PaneelLayoutService.MinPaneelMaat)
    {
        var hoogtes = deelHoogtes.ToList();
        var totaalHoogte = Math.Round(hoogtes.Sum(), 1);
        var restantHoogte = Math.Round(beschikbareHoogte - totaalHoogte, 1);
        var heeftGeldigeDeelHoogtes = hoogtes.Count > 1 && hoogtes.All(hoogte => hoogte >= minimumDeelHoogte - 0.001);
        var heeftGeldigeSom = Math.Abs(restantHoogte) < 0.001;

        return new PaneelOpdeelAnalyse(
            BeschikbareHoogte: Math.Round(beschikbareHoogte, 1),
            TotaalHoogte: totaalHoogte,
            RestantHoogte: restantHoogte,
            HeeftGeldigeDeelHoogtes: heeftGeldigeDeelHoogtes,
            HeeftGeldigeSom: heeftGeldigeSom);
    }

    public static List<PaneelRechthoek> BouwOpdeelSegmenten(PaneelRechthoek bereik, IEnumerable<double> deelHoogtes)
    {
        var segmenten = new List<PaneelRechthoek>();
        var onderkant = bereik.HoogteVanVloer;

        foreach (var hoogte in deelHoogtes)
        {
            segmenten.Add(new PaneelRechthoek
            {
                XPositie = Math.Round(bereik.XPositie, 1),
                HoogteVanVloer = Math.Round(onderkant, 1),
                Breedte = Math.Round(bereik.Breedte, 1),
                Hoogte = Math.Round(hoogte, 1)
            });

            onderkant += hoogte;
        }

        return segmenten;
    }

    public static string VrijSegmentLabel(int index, PaneelRechthoek segment)
        => $"Vak {index + 1} · {segment.Hoogte:0.#} hoog · onder {segment.HoogteVanVloer:0.#}";

    /// <summary>
    /// Verdeelt <paramref name="beschikbareHoogte"/> over <paramref name="aantal"/> panelen zodanig
    /// dat het onderlinge verschil nooit meer dan 1 mm bedraagt. Wanneer de totale hoogte geen
    /// heel getal is wordt de fractie aan het laatste paneel toegevoegd (terugval op oud gedrag).
    /// </summary>
    public static List<double> MaakStandaardOpdeelHoogtes(double beschikbareHoogte, int aantal)
    {
        if (aantal <= 0)
            return [];

        var totaalGerond = (int)Math.Round(beschikbareHoogte, 0, MidpointRounding.AwayFromZero);

        // Fractional total: put remainder on last panel (can't round all to integers)
        if (Math.Abs(beschikbareHoogte - totaalGerond) > 0.001)
        {
            var basis = Math.Floor(beschikbareHoogte / aantal);
            var hoogtes = Enumerable.Repeat(basis, aantal).ToList();
            hoogtes[^1] = Math.Round(beschikbareHoogte - basis * (aantal - 1), 1);
            return hoogtes;
        }

        // Integer total: spread remainder across panels so max difference is 1 mm
        var basisMm = totaalGerond / aantal;
        var aantalGroter = totaalGerond % aantal;
        return [.. Enumerable.Range(0, aantal)
            .Select(i => (double)(i < aantal - aantalGroter ? basisMm : basisMm + 1))];
    }

    public static string TypeBadgeClass(PaneelType type)
        => type switch
        {
            PaneelType.Deur => "bg-primary",
            PaneelType.LadeFront => "bg-warning text-dark",
            _ => "bg-secondary"
        };

    private static double SnapEdge(double waarde, IEnumerable<double> targets, double drempel)
    {
        var best = targets
            .Select(target => new { target, diff = Math.Abs(target - waarde) })
            .OrderBy(item => item.diff)
            .FirstOrDefault();

        return best is not null && best.diff <= drempel
            ? Math.Round(best.target)
            : RondRaster(waarde);
    }

    private static double SnapPositieMetBehoudVanMaat(double waarde, double maat, IEnumerable<double> targets, double drempel)
    {
        var kandidaten = targets
            .SelectMany(target => new[] { target, target - maat })
            .Distinct()
            .Select(target => new { target, diff = Math.Abs(target - waarde) })
            .OrderBy(item => item.diff)
            .FirstOrDefault();

        return kandidaten is not null && kandidaten.diff <= drempel
            ? Math.Round(kandidaten.target)
            : RondRaster(waarde);
    }

    private static PaneelRechthoek BouwRechthoek(double left, double right, double bottom, double top, PaneelRechthoek selectieBereik)
    {
        var minBreedte = Math.Min(PaneelLayoutService.MinPaneelMaat, selectieBereik.Breedte);
        var minHoogte = Math.Min(PaneelLayoutService.MinPaneelMaat, selectieBereik.Hoogte);

        left = Math.Clamp(left, selectieBereik.XPositie, selectieBereik.Rechterkant - minBreedte);
        right = Math.Clamp(right, left + minBreedte, selectieBereik.Rechterkant);
        bottom = Math.Clamp(bottom, selectieBereik.HoogteVanVloer, selectieBereik.Bovenzijde - minHoogte);
        top = Math.Clamp(top, bottom + minHoogte, selectieBereik.Bovenzijde);

        return new PaneelRechthoek
        {
            XPositie = RondRaster(left),
            HoogteVanVloer = RondRaster(bottom),
            Breedte = RondRaster(Math.Max(minBreedte, right - left)),
            Hoogte = RondRaster(Math.Max(minHoogte, top - bottom))
        };
    }

    private static double RondRaster(double waarde) => Math.Round(waarde / 10.0) * 10.0;

    private static bool HeeftHorizontaleOverlap(PaneelRechthoek links, PaneelRechthoek rechts)
    {
        const double tolerantie = 1.0;
        return Math.Min(links.Rechterkant, rechts.Rechterkant) - Math.Max(links.XPositie, rechts.XPositie) > tolerantie;
    }
}

public sealed record PaneelFlowStap(string Id, string Titel, string Beschrijving);

public readonly record struct PaneelOpdeelAnalyse(
    double BeschikbareHoogte,
    double TotaalHoogte,
    double RestantHoogte,
    bool HeeftGeldigeDeelHoogtes,
    bool HeeftGeldigeSom)
{
    public bool KanBevestigen => HeeftGeldigeDeelHoogtes && HeeftGeldigeSom;
}

public readonly record struct PaneelFlowContext(
    bool HeeftWandContext,
    bool HeeftSelectie,
    bool HeeftConceptPaneel,
    bool HeeftGeldigeMaat,
    bool RaaktGeselecteerdeKast,
    bool HeeftConflicterendPaneel,
    string ActieveWandNaam,
    string GeselecteerdeKastNamen);
