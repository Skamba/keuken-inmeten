namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class WandOpstellingHelper
{
    private const double RasterMm = 10.0;
    private const double SnapDrempel = 20.0;
    private const double StandaardGaatjesAfstand = 32.0;

    public static IReadOnlyList<int> BepaalHoogteMarkeringen(double wandHoogte)
        => BepaalMarkeringen(wandHoogte);

    public static IReadOnlyList<int> BepaalBreedteMarkeringen(double wandBreedte)
        => BepaalMarkeringen(wandBreedte);

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
        var andereKastLijst = andereKasten.ToList();

        xPositie = SnapValue(xPositie, BepaalXSnapTargets(kast, andereKastLijst, wandBreedte), SnapDrempel);
        hoogteVanVloer = SnapValue(hoogteVanVloer, BepaalYSnapTargets(kast, andereKastLijst, plintHoogte), SnapDrempel);

        return new WandPositie(
            Math.Clamp(xPositie, 0, Math.Max(0, wandBreedte - kast.Breedte)),
            Math.Clamp(hoogteVanVloer, 0, Math.Max(0, wandHoogte - kast.Hoogte)));
    }

    public static double BepaalPlankHoogteNaDrop(Kast kast, double svgCenterY, double vloerY, double schaal)
    {
        var kastBotY = vloerY - kast.HoogteVanVloer * schaal;
        var wanddiktePx = kast.Wanddikte * schaal;
        var hoogteVanBodem = (kastBotY - wanddiktePx - svgCenterY) / schaal;

        return Math.Round(
            Math.Clamp(hoogteVanBodem, kast.Wanddikte, kast.Hoogte - kast.Wanddikte * 2),
            1);
    }

    public static double BepaalPlankHoogteVoorToevoegen(Kast kast, double svgY, double vloerY, double schaal)
    {
        var kastBotY = vloerY - kast.HoogteVanVloer * schaal;
        var wanddiktePx = kast.Wanddikte * schaal;
        var hoogteVanBodem = (kastBotY - wanddiktePx - svgY) / schaal;
        var gaatjesAfstand = kast.GaatjesAfstand > 0 ? kast.GaatjesAfstand : StandaardGaatjesAfstand;

        hoogteVanBodem = Math.Round(hoogteVanBodem / gaatjesAfstand) * gaatjesAfstand;
        return Math.Clamp(Math.Round(hoogteVanBodem, 1), kast.Wanddikte, kast.Hoogte - kast.Wanddikte * 2);
    }

    public static double? BepaalPlankHoogteNaToets(Kast kast, Plank plank, string key, double stap)
        => key switch
        {
            "ArrowUp" => Math.Min(kast.Hoogte - kast.Wanddikte * 2, plank.HoogteVanBodem + stap),
            "ArrowDown" => Math.Max(kast.Wanddikte, plank.HoogteVanBodem - stap),
            _ => null
        };

    public static WandPositie? BepaalKastPositieNaToets(Kast kast, string key, double stap, double wandBreedte, double wandHoogte)
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

        return new WandPositie(xPositie, hoogteVanVloer);
    }

    private static IReadOnlyList<int> BepaalMarkeringen(double maat)
    {
        var step = maat > 2000 ? 500 : 250;
        var markeringen = new List<int>();
        for (var waarde = step; waarde < maat; waarde += step)
            markeringen.Add(waarde);

        return markeringen;
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

    private static double ClampOpRaster(double waarde)
        => Math.Round(waarde / RasterMm) * RasterMm;

    private static double ClampOpRaster(double waarde, double minimum, double maximum)
        => Math.Clamp(ClampOpRaster(waarde), minimum, maximum);
}

public readonly record struct WandPositie(double XPositie, double HoogteVanVloer);
