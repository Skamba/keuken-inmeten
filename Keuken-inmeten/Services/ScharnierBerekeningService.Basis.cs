namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class ScharnierBerekeningService
{
    public static double NormaliseerCupCenterVanRand(double waarde)
    {
        if (double.IsNaN(waarde) || double.IsInfinity(waarde))
            return CupCenterVanRand;

        return Math.Round(Math.Max(MinCupCenterVanRand, waarde), 1);
    }

    /// <summary>
    /// Geeft alle gaatjesrij-posities van bovenkant kast.
    /// </summary>
    public static List<double> GaatjesRijPosities(double hoogte, double eersteGaatVanBoven, double gaatjesAfstand)
    {
        if (gaatjesAfstand <= 0)
            return [];

        var posities = new List<double>();
        var pos = eersteGaatVanBoven;
        while (pos < hoogte - 5)
        {
            posities.Add(pos);
            pos += gaatjesAfstand;
        }

        return posities;
    }

    /// <summary>
    /// Berekent alle mogelijke montageplaat-centra: het midden tussen twee opeenvolgende gaten.
    /// </summary>
    public static List<double> MontagePlaatCentra(double hoogte, double eersteGaatVanBoven, double gaatjesAfstand)
    {
        var gaten = GaatjesRijPosities(hoogte, eersteGaatVanBoven, gaatjesAfstand);
        var centra = new List<double>();
        for (var i = 0; i < gaten.Count - 1; i++)
        {
            centra.Add((gaten[i] + gaten[i + 1]) / 2.0);
        }

        return centra;
    }

    /// <summary>
    /// Bepaalt het aantal scharnieren op basis van paneelhoogte.
    /// </summary>
    public static int AantalScharnieren(double hoogte) => hoogte switch
    {
        <= 1000 => 2,
        <= 1500 => 3,
        <= 2200 => 4,
        _ => 5
    };

    /// <summary>
    /// Berekent standaard scharnierposities, gesnapt naar gaatjesrij-midpoints.
    /// </summary>
    public static List<double> BerekenStandaardPosities(
        double kastHoogte,
        double eersteGaatVanBoven,
        double gaatjesAfstand)
    {
        var centra = MontagePlaatCentra(kastHoogte, eersteGaatVanBoven, gaatjesAfstand);
        if (centra.Count == 0)
            return [];

        var aantal = Math.Min(AantalScharnieren(kastHoogte), centra.Count);
        var tussenruimte = (kastHoogte - 2 * MinAfstandVanRand) / Math.Max(aantal - 1, 1);
        var gekozen = new List<double>();
        var beschikbaar = new List<double>(centra);

        for (var i = 0; i < aantal; i++)
        {
            var ideaal = MinAfstandVanRand + i * tussenruimte;
            var dichtsbij = beschikbaar.OrderBy(midden => Math.Abs(midden - ideaal)).First();
            gekozen.Add(dichtsbij);
            beschikbaar.Remove(dichtsbij);
        }

        return gekozen.OrderBy(positie => positie).Select(positie => Math.Round(positie, 1)).ToList();
    }

    /// <summary>
    /// Fallback: berekent posities zonder gaatjesrij (vrije plaatsing).
    /// </summary>
    public static List<double> BerekenStandaardPosities(double hoogte)
    {
        var aantal = AantalScharnieren(hoogte);
        var tussenruimte = (hoogte - 2 * MinAfstandVanRand) / Math.Max(aantal - 1, 1);
        var posities = new List<double>(aantal);

        for (var i = 0; i < aantal; i++)
        {
            posities.Add(Math.Round(MinAfstandVanRand + i * tussenruimte, 1));
        }

        return posities;
    }

    /// <summary>
    /// Bepaalt of kasten verticaal gestapeld zijn (boven elkaar) of naast elkaar.
    /// Verticaal: overlappende X-range + verschillende vloerhoogte, of Bovenkast-combinatie.
    /// </summary>
    public static bool IsVertikaalGestapeld(List<Kast> kasten)
    {
        if (kasten.Count < 2)
            return false;

        for (var i = 0; i < kasten.Count; i++)
        {
            for (var j = i + 1; j < kasten.Count; j++)
            {
                var a = kasten[i];
                var b = kasten[j];
                var overlapsX = a.XPositie < b.XPositie + b.Breedte && b.XPositie < a.XPositie + a.Breedte;
                var differentFloor = Math.Abs(a.HoogteVanVloer - b.HoogteVanVloer) > 1;
                if (overlapsX && differentFloor)
                    return true;
            }
        }

        var hasBovenKast = kasten.Any(kast => kast.Type == KastType.Bovenkast);
        var sameWidth = kasten.Select(kast => (int)kast.Breedte).Distinct().Count() == 1;
        return hasBovenKast && sameWidth;
    }

    /// <summary>
    /// Berekent de paneelafmetingen op basis van de kasten-opstelling:
    /// gestapeld → breedte = max, hoogte = som; naast elkaar → breedte = som, hoogte = max.
    /// </summary>
    public static (double breedte, double hoogte) BerekenPaneelAfmetingen(List<Kast> kasten)
    {
        if (kasten.Count == 0)
            return (0, 0);

        if (kasten.Count == 1)
            return (kasten[0].Breedte, kasten[0].Hoogte);

        return IsVertikaalGestapeld(kasten)
            ? (kasten.Max(kast => kast.Breedte), kasten.Sum(kast => kast.Hoogte))
            : (kasten.Sum(kast => kast.Breedte), kasten.Max(kast => kast.Hoogte));
    }
}
