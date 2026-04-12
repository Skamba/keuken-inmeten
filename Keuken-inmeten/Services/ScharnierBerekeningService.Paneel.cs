namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class ScharnierBerekeningService
{
    /// <summary>
    /// Berekent scharnierposities voor een paneel op basis van de kasten hun gaatjesrij.
    /// Posities worden gesnapt naar gaatjesrij-midpoints en relatief gemaakt aan het paneel.
    /// Bij gestapelde kasten worden posities die te dicht bij een overgang liggen vermeden.
    /// </summary>
    public static List<double> BerekenPaneelScharnierPosities(
        List<Kast> kasten,
        double paneelHoogte,
        ScharnierZijde zijde)
        => BerekenPaneelBoorgaten(kasten, paneelHoogte, zijde).Select(boorgat => boorgat.Y).ToList();

    public static List<Boorgat> BerekenPaneelBoorgaten(
        List<Kast> kasten,
        double paneelHoogte,
        ScharnierZijde zijde)
    {
        var segmenten = BouwPaneelSegmenten(kasten, paneelHoogte, zijde);
        return BerekenPaneelBoorgaten(segmenten, paneelHoogte, CupCenterVanRand);
    }

    public static PaneelResultaat BerekenPaneel(PaneelToewijzing toewijzing, List<Kast> kasten)
        => BerekenPaneel(toewijzing, kasten, null);

    public static PaneelResultaat BerekenPaneel(PaneelToewijzing toewijzing, List<Kast> kasten, PaneelMaatInfo? maatInfo)
    {
        var boorgaten = toewijzing.Type == PaneelType.Deur
            ? BerekenPaneelBoorgaten(toewijzing, kasten, maatInfo)
            : [];

        return new PaneelResultaat
        {
            ToewijzingId = toewijzing.Id,
            KastIds = toewijzing.KastIds,
            KastNaam = string.Join(" + ", kasten.Select(kast => kast.Naam)),
            Type = toewijzing.Type,
            Breedte = maatInfo?.PaneelRechthoek.Breedte ?? toewijzing.Breedte,
            Hoogte = maatInfo?.PaneelRechthoek.Hoogte ?? toewijzing.Hoogte,
            ScharnierZijde = toewijzing.ScharnierZijde,
            MaatInfo = maatInfo,
            Boorgaten = boorgaten
        };
    }

    public static List<PaneelSegmentInfo> BerekenPaneelSegmenten(PaneelToewijzing toewijzing, List<Kast> kasten)
        => BerekenPaneelSegmenten(toewijzing, kasten, null);

    public static List<PaneelSegmentInfo> BerekenPaneelSegmenten(PaneelToewijzing toewijzing, List<Kast> kasten, PaneelMaatInfo? maatInfo)
    {
        List<PaneelSegment> segmenten;
        if (maatInfo?.PaneelRechthoek is not null)
        {
            segmenten = BouwPaneelSegmenten(kasten, maatInfo.PaneelRechthoek, toewijzing.ScharnierZijde);
        }
        else if (toewijzing.XPositie is null || toewijzing.HoogteVanVloer is null)
        {
            segmenten = BouwPaneelSegmenten(kasten, toewijzing.Hoogte, toewijzing.ScharnierZijde);
        }
        else
        {
            var paneel = PaneelLayoutService.BerekenRechthoek(toewijzing, kasten);
            segmenten = paneel is null
                ? []
                : BouwPaneelSegmenten(kasten, paneel, toewijzing.ScharnierZijde);
        }

        return segmenten
            .Select(segment => new PaneelSegmentInfo
            {
                Kast = segment.Kast,
                TopVanPaneel = segment.TopVanPaneel,
                KastOffsetVanBoven = segment.KastOffsetVanBoven,
                Hoogte = segment.Hoogte
            })
            .ToList();
    }

    private static List<Boorgat> BerekenPaneelBoorgaten(List<PaneelSegment> segmenten, double paneelHoogte, double potHartVanRand)
    {
        var kandidaten = BerekenScharnierKandidaten(segmenten);
        if (kandidaten.Count == 0)
            return [];

        var junctiesVanBoven = segmenten
            .Take(Math.Max(segmenten.Count - 1, 0))
            .Select(segment => segment.TopVanPaneel + segment.Hoogte)
            .ToList();
        var plankPositiesVanBoven = segmenten
            .SelectMany(segment => segment.Kast.Planken.Select(plank =>
                segment.TopVanPaneel + (PlankGaatjesHelper.BepaalHoogteVanBoven(segment.Kast, plank.HoogteVanBodem) - segment.KastOffsetVanBoven)))
            .Where(plankY => plankY >= 0 && plankY <= paneelHoogte)
            .ToList();

        const double junctionZone = 50.0;
        const double plankZone = 30.0;

        var beschikbaar = kandidaten
            .Where(kandidaat => kandidaat.PaneelY >= MinAfstandVanRand && kandidaat.PaneelY <= paneelHoogte - MinAfstandVanRand)
            .Where(kandidaat => !junctiesVanBoven.Any(junctie => Math.Abs(kandidaat.PaneelY - junctie) < junctionZone))
            .Where(kandidaat => !plankPositiesVanBoven.Any(plankPositie => Math.Abs(kandidaat.PaneelY - plankPositie) < plankZone))
            .ToList();

        if (beschikbaar.Count == 0)
        {
            beschikbaar = kandidaten
                .Where(kandidaat => kandidaat.PaneelY >= 0 && kandidaat.PaneelY <= paneelHoogte)
                .ToList();
        }

        if (beschikbaar.Count == 0)
            return [];

        var aantal = Math.Min(AantalScharnieren(paneelHoogte), beschikbaar.Count);
        var tussenruimte = (paneelHoogte - 2 * MinAfstandVanRand) / Math.Max(aantal - 1, 1);
        var gekozen = new List<(ScharnierKandidaat kandidaat, double ideaal)>();
        var pool = new List<ScharnierKandidaat>(beschikbaar);

        for (var i = 0; i < aantal; i++)
        {
            var ideaal = MinAfstandVanRand + i * tussenruimte;
            var dichtsbij = pool.OrderBy(kandidaat => Math.Abs(kandidaat.PaneelY - ideaal)).First();
            gekozen.Add((dichtsbij, ideaal));
            pool.Remove(dichtsbij);
        }

        return gekozen
            .OrderBy(item => item.kandidaat.PaneelY)
            .Select(item => MaakBoorgat(item.kandidaat, paneelHoogte, potHartVanRand, item.ideaal, junctiesVanBoven, plankPositiesVanBoven))
            .ToList();
    }

    private static List<Boorgat> BerekenPaneelBoorgaten(PaneelToewijzing toewijzing, List<Kast> kasten, PaneelMaatInfo? maatInfo)
    {
        var potHartVanRand = NormaliseerCupCenterVanRand(toewijzing.PotHartVanRand);

        if (maatInfo?.PaneelRechthoek is not null)
        {
            var fysiekeSegmenten = BouwPaneelSegmenten(kasten, maatInfo.PaneelRechthoek, toewijzing.ScharnierZijde);
            return BerekenPaneelBoorgaten(fysiekeSegmenten, maatInfo.PaneelRechthoek.Hoogte, potHartVanRand);
        }

        if (toewijzing.XPositie is null || toewijzing.HoogteVanVloer is null)
        {
            var segmentenZonderRechthoek = BouwPaneelSegmenten(kasten, toewijzing.Hoogte, toewijzing.ScharnierZijde);
            return BerekenPaneelBoorgaten(segmentenZonderRechthoek, toewijzing.Hoogte, potHartVanRand);
        }

        var paneel = PaneelLayoutService.BerekenRechthoek(toewijzing, kasten);
        if (paneel is null)
            return [];

        var segmenten = BouwPaneelSegmenten(kasten, paneel, toewijzing.ScharnierZijde);
        return BerekenPaneelBoorgaten(segmenten, paneel.Hoogte, potHartVanRand);
    }

    private static List<PaneelSegment> BouwPaneelSegmenten(List<Kast> kasten, double paneelHoogte, ScharnierZijde zijde)
    {
        if (kasten.Count == 0)
            return [];

        if (IsVertikaalGestapeld(kasten))
        {
            var gesorteerd = kasten
                .OrderByDescending(kast => kast.HoogteVanVloer)
                .ThenBy(kast => kast.Type == KastType.Bovenkast ? 0 : 1)
                .ThenBy(kast => kast.XPositie)
                .ToList();

            var segmenten = new List<PaneelSegment>();
            var topVanPaneel = 0.0;
            foreach (var kast in gesorteerd)
            {
                segmenten.Add(new PaneelSegment(kast, topVanPaneel, 0, kast.Hoogte));
                topVanPaneel += kast.Hoogte;
            }

            return segmenten;
        }

        var draagKast = zijde == ScharnierZijde.Links
            ? kasten.OrderBy(kast => kast.XPositie).ThenBy(kast => kast.Naam).First()
            : kasten.OrderByDescending(kast => kast.XPositie + kast.Breedte).ThenBy(kast => kast.Naam).First();

        return [new PaneelSegment(draagKast, Math.Max(0, paneelHoogte - draagKast.Hoogte), 0, draagKast.Hoogte)];
    }

    private static List<PaneelSegment> BouwPaneelSegmenten(List<Kast> kasten, PaneelRechthoek paneel, ScharnierZijde zijde)
    {
        if (kasten.Count == 0)
            return [];

        var probeOffset = zijde == ScharnierZijde.Links ? 1.0 : -1.0;
        var probeX = Math.Clamp(
            zijde == ScharnierZijde.Links ? paneel.XPositie + probeOffset : paneel.Rechterkant + probeOffset,
            paneel.XPositie + 0.1,
            paneel.Rechterkant - 0.1);

        var panelTop = paneel.Bovenzijde;
        var panelBottom = paneel.HoogteVanVloer;

        var segmenten = kasten
            .Where(kast => probeX >= kast.XPositie - 0.1 && probeX <= kast.XPositie + kast.Breedte + 0.1)
            .Select(kast =>
            {
                var kastTop = kast.HoogteVanVloer + kast.Hoogte;
                var overlapTop = Math.Min(panelTop, kastTop);
                var overlapBottom = Math.Max(panelBottom, kast.HoogteVanVloer);
                var overlapHoogte = overlapTop - overlapBottom;
                if (overlapHoogte < 1.0)
                    return null;

                return new PaneelSegment(
                    kast,
                    panelTop - overlapTop,
                    kastTop - overlapTop,
                    overlapHoogte);
            })
            .Where(segment => segment is not null)
            .Cast<PaneelSegment>()
            .OrderBy(segment => segment.TopVanPaneel)
            .ThenBy(segment => segment.Kast.XPositie)
            .ToList();

        return segmenten.Count > 0
            ? segmenten
            : BouwPaneelSegmenten(kasten, paneel.Hoogte, zijde);
    }

    private static List<ScharnierKandidaat> BerekenScharnierKandidaten(List<PaneelSegment> segmenten)
    {
        var kandidaten = new List<ScharnierKandidaat>();

        foreach (var segment in segmenten)
        {
            if (segment.Kast.GaatjesAfstand <= 0)
                continue;

            var gaten = GaatjesRijPosities(segment.Kast.Hoogte, segment.Kast.EersteGaatPositieVanafBoven, segment.Kast.GaatjesAfstand);
            var bezetteGaten = PlankGaatjesHelper.BepaalBezetteGatenVanBoven(segment.Kast);
            for (var i = 0; i < gaten.Count - 1; i++)
            {
                if (bezetteGaten.Any(bezetteY => Math.Abs(bezetteY - gaten[i]) < 0.5 || Math.Abs(bezetteY - gaten[i + 1]) < 0.5))
                    continue;

                var montagePlaatMidden = (gaten[i] + gaten[i + 1]) / 2.0;
                if (montagePlaatMidden < segment.KastOffsetVanBoven - 0.1
                    || montagePlaatMidden > segment.KastOffsetVanBoven + segment.Hoogte + 0.1)
                {
                    continue;
                }

                kandidaten.Add(new ScharnierKandidaat(
                    segment.Kast,
                    segment.TopVanPaneel + (montagePlaatMidden - segment.KastOffsetVanBoven),
                    montagePlaatMidden,
                    i + 1,
                    i + 2,
                    gaten[i],
                    gaten[i + 1]));
            }
        }

        return kandidaten.OrderBy(kandidaat => kandidaat.PaneelY).ToList();
    }

    private static Boorgat MaakBoorgat(
        ScharnierKandidaat kandidaat,
        double paneelHoogte,
        double potHartVanRand,
        double idealeY,
        List<double> junctiesVanBoven,
        List<double> plankPositiesVanBoven)
        => new()
        {
            X = potHartVanRand,
            Y = Math.Round(kandidaat.PaneelY, 1),
            Diameter = CupDiameter,
            Onderbouwing = new BoorgatOnderbouwing
            {
                KastNaam = kandidaat.Kast.Naam,
                GaatBovenIndex = kandidaat.GaatBovenIndex,
                GaatOnderIndex = kandidaat.GaatOnderIndex,
                GaatBovenY = kandidaat.GaatBovenY,
                GaatOnderY = kandidaat.GaatOnderY,
                MontagePlaatMiddenInKast = Math.Round(kandidaat.MontagePlaatMiddenInKast, 1),
                IdealeY = Math.Round(idealeY, 1),
                AfstandTotIdealeVerdeling = Math.Round(Math.Abs(kandidaat.PaneelY - idealeY), 1),
                AfstandTotBoven = Math.Round(kandidaat.PaneelY, 1),
                AfstandTotOnder = Math.Round(paneelHoogte - kandidaat.PaneelY, 1),
                AfstandTotDichtstbijzijndeNaad = MinAfstandTot(kandidaat.PaneelY, junctiesVanBoven),
                AfstandTotDichtstbijzijndePlank = MinAfstandTot(kandidaat.PaneelY, plankPositiesVanBoven)
            }
        };

    private static double? MinAfstandTot(double positie, List<double> targets)
        => targets.Count == 0 ? null : Math.Round(targets.Min(target => Math.Abs(positie - target)), 1);
}
