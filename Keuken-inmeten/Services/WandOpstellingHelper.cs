namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class WandOpstellingHelper
{
    private const double RasterMm = 10.0;
    private const double SnapDrempel = 25.0;
    private const double AansluitingTolerantie = 0.6;

    public static IReadOnlyList<int> BepaalHoogteMarkeringen(double wandHoogte)
        => BepaalMarkeringen(wandHoogte);

    public static IReadOnlyList<int> BepaalBreedteMarkeringen(double wandBreedte)
        => BepaalMarkeringen(wandBreedte);

    /// <summary>
    /// Geeft alle aansluitpunten terug als lijnstukken (in mm-coördinaten van de wand)
    /// die getoond kunnen worden als visuele indicatie dat kasten naadloos aansluiten.
    /// Elke entry: (x1, y1Vloer, x2, y2Vloer) — het gedeelde rand-segment.
    /// </summary>
    public static IReadOnlyList<(double X1, double Y1, double X2, double Y2)> BepaalAansluitingen(
        IEnumerable<Kast> kastenBron)
    {
        const double minOverlap = 1.0;
        var kasten = kastenBron.ToList();
        var resultaat = new List<(double, double, double, double)>();
        var gezien = new HashSet<(int, int)>();

        for (var i = 0; i < kasten.Count; i++)
        {
            for (var j = i + 1; j < kasten.Count; j++)
            {
                if (!gezien.Add((i, j)))
                    continue;

                var a = kasten[i];
                var b = kasten[j];

                // Horizontale aansluiting: a's bovenkant raakt b's onderkant of vice versa
                var hOverlap = Math.Min(a.XPositie + a.Breedte, b.XPositie + b.Breedte)
                             - Math.Max(a.XPositie, b.XPositie);

                if (hOverlap >= minOverlap)
                {
                    var aBoven = a.HoogteVanVloer + a.Hoogte;
                    var bBoven = b.HoogteVanVloer + b.Hoogte;

                    if (Math.Abs(aBoven - b.HoogteVanVloer) <= AansluitingTolerantie)
                    {
                        var x1 = Math.Max(a.XPositie, b.XPositie);
                        var x2 = Math.Min(a.XPositie + a.Breedte, b.XPositie + b.Breedte);
                        resultaat.Add((x1, aBoven, x2, aBoven));
                        continue;
                    }

                    if (Math.Abs(bBoven - a.HoogteVanVloer) <= AansluitingTolerantie)
                    {
                        var x1 = Math.Max(a.XPositie, b.XPositie);
                        var x2 = Math.Min(a.XPositie + a.Breedte, b.XPositie + b.Breedte);
                        resultaat.Add((x1, bBoven, x2, bBoven));
                        continue;
                    }
                }

                // Verticale aansluiting: a's rechterkant raakt b's linkerkant of vice versa
                var vOverlap = Math.Min(a.HoogteVanVloer + a.Hoogte, b.HoogteVanVloer + b.Hoogte)
                             - Math.Max(a.HoogteVanVloer, b.HoogteVanVloer);

                if (vOverlap >= minOverlap)
                {
                    var aRechts = a.XPositie + a.Breedte;
                    var bRechts = b.XPositie + b.Breedte;

                    if (Math.Abs(aRechts - b.XPositie) <= AansluitingTolerantie)
                    {
                        var y1 = Math.Max(a.HoogteVanVloer, b.HoogteVanVloer);
                        var y2 = Math.Min(a.HoogteVanVloer + a.Hoogte, b.HoogteVanVloer + b.Hoogte);
                        resultaat.Add((aRechts, y1, aRechts, y2));
                        continue;
                    }

                    if (Math.Abs(bRechts - a.XPositie) <= AansluitingTolerantie)
                    {
                        var y1 = Math.Max(a.HoogteVanVloer, b.HoogteVanVloer);
                        var y2 = Math.Min(a.HoogteVanVloer + a.Hoogte, b.HoogteVanVloer + b.Hoogte);
                        resultaat.Add((bRechts, y1, bRechts, y2));
                        continue;
                    }
                }
            }
        }

        return resultaat;
    }

    public static WandPositie BepaalApparaatPositieNaDrop(
        Apparaat apparaat,
        double svgX,
        double svgY,
        double padding,
        double schaal,
        double vloerY,
        double wandBreedte,
        double wandHoogte)
    {
        var xPositie = (svgX - padding) / schaal;
        var topFromFloorMm = (vloerY - svgY) / schaal;
        var hoogteVanVloer = topFromFloorMm - apparaat.Hoogte;

        return new WandPositie(
            ClampOpRaster(xPositie, 0, Math.Max(0, wandBreedte - apparaat.Breedte)),
            ClampOpRaster(hoogteVanVloer, 0, Math.Max(0, wandHoogte - apparaat.Hoogte)));
    }

    public static WandPositie BepaalKastPositieNaDrop(
        Kast kast,
        IEnumerable<Kast> andereKasten,
        double svgX,
        double svgY,
        double padding,
        double schaal,
        double vloerY,
        double wandBreedte,
        double wandHoogte,
        double plintHoogte)
    {
        var xPositie = (svgX - padding) / schaal;
        var topFromFloorMm = (vloerY - svgY) / schaal;
        var hoogteVanVloer = topFromFloorMm - kast.Hoogte;
        return SnapKastPositie(
            kast,
            andereKasten,
            xPositie,
            hoogteVanVloer,
            wandBreedte,
            wandHoogte,
            plintHoogte);
    }

    public static double BepaalPlankHoogteNaDrop(Kast kast, double svgCenterY, double vloerY, double schaal)
    {
        var kastBotY = vloerY - kast.HoogteVanVloer * schaal;
        var wanddiktePx = kast.Wanddikte * schaal;
        var hoogteVanBodem = (kastBotY - wanddiktePx - svgCenterY) / schaal;

        return PlankGaatjesHelper.SnapHoogteVanBodem(kast, hoogteVanBodem);
    }

    public static double BepaalPlankHoogteVoorToevoegen(Kast kast, double svgY, double vloerY, double schaal)
    {
        var kastBotY = vloerY - kast.HoogteVanVloer * schaal;
        var wanddiktePx = kast.Wanddikte * schaal;
        var hoogteVanBodem = (kastBotY - wanddiktePx - svgY) / schaal;

        return PlankGaatjesHelper.SnapHoogteVanBodem(kast, hoogteVanBodem);
    }

    public static double? BepaalPlankHoogteNaToets(Kast kast, Plank plank, string key, double stap)
        => key switch
        {
            "ArrowUp" => PlankGaatjesHelper.BepaalVolgendeSnapHoogteVanBodem(kast, plank.HoogteVanBodem, omhoog: true)
                ?? Math.Min(kast.Hoogte - kast.Wanddikte * 2, plank.HoogteVanBodem + stap),
            "ArrowDown" => PlankGaatjesHelper.BepaalVolgendeSnapHoogteVanBodem(kast, plank.HoogteVanBodem, omhoog: false)
                ?? Math.Max(kast.Wanddikte, plank.HoogteVanBodem - stap),
            _ => null
        };

    public static WandPositie? BepaalKastPositieNaToets(
        Kast kast,
        IEnumerable<Kast> andereKasten,
        string key,
        double stap,
        double wandBreedte,
        double wandHoogte,
        double plintHoogte)
    {
        var xPositie = kast.XPositie;
        var hoogteVanVloer = kast.HoogteVanVloer;

        switch (key)
        {
            case "ArrowLeft":
                xPositie = Math.Max(0, kast.XPositie - stap);
                break;
            case "ArrowRight":
                xPositie = Math.Min(wandBreedte - kast.Breedte, kast.XPositie + stap);
                break;
            case "ArrowUp":
                hoogteVanVloer = Math.Min(wandHoogte - kast.Hoogte, kast.HoogteVanVloer + stap);
                break;
            case "ArrowDown":
                hoogteVanVloer = Math.Max(0, kast.HoogteVanVloer - stap);
                break;
            default:
                return null;
        }

        return SnapKastPositie(
            kast,
            andereKasten,
            xPositie,
            hoogteVanVloer,
            wandBreedte,
            wandHoogte,
            plintHoogte);
    }

    private static IReadOnlyList<int> BepaalMarkeringen(double maat)
    {
        var step = maat > 2000 ? 500 : 250;
        var markeringen = new List<int>();
        for (var waarde = step; waarde < maat; waarde += step)
            markeringen.Add(waarde);

        return markeringen;
    }

    /// <summary>
    /// Bepaalt kleine gaten (0 &lt; gap &lt; RasterMm) die zijn ontstaan nadat
    /// <paramref name="gewijzigdeKast"/> werd bijgewerkt. Geeft voor elke
    /// aangrenzende kast de nieuwe positie zodat het gat gesloten wordt.
    /// </summary>
    public static IReadOnlyList<(Guid KastId, double XPositie, double HoogteVanVloer)> BepaalGatSluitingen(
        Kast gewijzigdeKast,
        IEnumerable<Kast> andereKasten)
    {
        const double minGap = 0.001;
        const double maxGap = RasterMm + 0.001;
        const double minOverlap = 1.0;

        var resultaat = new List<(Guid, double, double)>();

        foreach (var andere in andereKasten)
        {
            var hOverlap = Math.Min(gewijzigdeKast.XPositie + gewijzigdeKast.Breedte, andere.XPositie + andere.Breedte)
                         - Math.Max(gewijzigdeKast.XPositie, andere.XPositie);

            if (hOverlap > minOverlap)
            {
                var gapBoven = andere.HoogteVanVloer - (gewijzigdeKast.HoogteVanVloer + gewijzigdeKast.Hoogte);
                if (gapBoven is > minGap and < maxGap)
                {
                    resultaat.Add((andere.Id, andere.XPositie, gewijzigdeKast.HoogteVanVloer + gewijzigdeKast.Hoogte));
                    continue;
                }

                var gapOnder = gewijzigdeKast.HoogteVanVloer - (andere.HoogteVanVloer + andere.Hoogte);
                if (gapOnder is > minGap and < maxGap)
                {
                    resultaat.Add((andere.Id, andere.XPositie, gewijzigdeKast.HoogteVanVloer - andere.Hoogte));
                    continue;
                }
            }

            var vOverlap = Math.Min(gewijzigdeKast.HoogteVanVloer + gewijzigdeKast.Hoogte, andere.HoogteVanVloer + andere.Hoogte)
                         - Math.Max(gewijzigdeKast.HoogteVanVloer, andere.HoogteVanVloer);

            if (vOverlap > minOverlap)
            {
                var gapRechts = andere.XPositie - (gewijzigdeKast.XPositie + gewijzigdeKast.Breedte);
                if (gapRechts is > minGap and < maxGap)
                {
                    resultaat.Add((andere.Id, gewijzigdeKast.XPositie + gewijzigdeKast.Breedte, andere.HoogteVanVloer));
                    continue;
                }

                var gapLinks = gewijzigdeKast.XPositie - (andere.XPositie + andere.Breedte);
                if (gapLinks is > minGap and < maxGap)
                {
                    resultaat.Add((andere.Id, gewijzigdeKast.XPositie - andere.Breedte, andere.HoogteVanVloer));
                    continue;
                }
            }
        }

        return resultaat;
    }

    private static IEnumerable<double> BepaalXSnapTargets(Kast kast, IEnumerable<Kast> andereKasten, double wandBreedte)
    {
        yield return 0;
        yield return Math.Max(0, wandBreedte - kast.Breedte);

        foreach (var andereKast in andereKasten)
        {
            yield return andereKast.XPositie;
            yield return andereKast.XPositie + andereKast.Breedte;
            yield return andereKast.XPositie - kast.Breedte;
        }
    }

    private static IEnumerable<double> BepaalYSnapTargets(Kast kast, IEnumerable<Kast> andereKasten, double plintHoogte)
    {
        yield return 0;
        yield return plintHoogte;

        foreach (var andereKast in andereKasten)
        {
            yield return andereKast.HoogteVanVloer;
            yield return andereKast.HoogteVanVloer + andereKast.Hoogte;
            yield return andereKast.HoogteVanVloer - kast.Hoogte;
        }
    }

    private static double SnapValue(double waarde, IEnumerable<double> targets, double drempel)
    {
        var best = targets.MinBy(target => Math.Abs(target - waarde));
        return Math.Abs(best - waarde) <= drempel
            ? Math.Round(best)
            : ClampOpRaster(waarde);
    }

    private static WandPositie SnapKastPositie(
        Kast kast,
        IEnumerable<Kast> andereKasten,
        double xPositie,
        double hoogteVanVloer,
        double wandBreedte,
        double wandHoogte,
        double plintHoogte)
    {
        var andereKastLijst = andereKasten.ToList();

        xPositie = SnapValue(xPositie, BepaalXSnapTargets(kast, andereKastLijst, wandBreedte), SnapDrempel);
        hoogteVanVloer = SnapValue(hoogteVanVloer, BepaalYSnapTargets(kast, andereKastLijst, plintHoogte), SnapDrempel);

        return new WandPositie(
            Math.Clamp(xPositie, 0, Math.Max(0, wandBreedte - kast.Breedte)),
            Math.Clamp(hoogteVanVloer, 0, Math.Max(0, wandHoogte - kast.Hoogte)));
    }

    private static double ClampOpRaster(double waarde)
        => Math.Round(waarde / RasterMm) * RasterMm;

    private static double ClampOpRaster(double waarde, double minimum, double maximum)
        => Math.Clamp(ClampOpRaster(waarde), minimum, maximum);
}

public readonly record struct WandPositie(double XPositie, double HoogteVanVloer);
