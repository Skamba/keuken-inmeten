namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class PlankGaatjesHelper
{
    private const double TolerantieMm = 0.5;

    public static IReadOnlyList<double> BepaalSnapHoogtesVanBodem(Kast kast)
        => BepaalGaatjesMetPlankHoogte(kast)
            .Select(item => item.HoogteVanBodem)
            .ToList();

    public static double SnapHoogteVanBodem(Kast kast, double hoogteVanBodem)
    {
        var (minimum, maximum) = BepaalPlankBereik(kast);
        var genormaliseerd = Math.Round(Math.Clamp(hoogteVanBodem, minimum, maximum), 1);
        var snapTargets = BepaalSnapHoogtesVanBodem(kast);
        if (snapTargets.Count == 0)
            return genormaliseerd;

        var dichtstbij = snapTargets.MinBy(target => Math.Abs(target - genormaliseerd));
        return Math.Round(dichtstbij, 1);
    }

    public static double? BepaalVolgendeSnapHoogteVanBodem(Kast kast, double huidigeHoogteVanBodem, bool omhoog)
    {
        var snapTargets = BepaalSnapHoogtesVanBodem(kast)
            .OrderBy(target => target)
            .ToList();

        if (snapTargets.Count == 0)
            return null;

        if (omhoog)
        {
            double? volgende = snapTargets
                .Where(target => target > huidigeHoogteVanBodem + TolerantieMm)
                .Select(target => (double?)target)
                .FirstOrDefault();
            return volgende ?? snapTargets[^1];
        }

        double? vorige = snapTargets
            .Where(target => target < huidigeHoogteVanBodem - TolerantieMm)
            .Select(target => (double?)target)
            .LastOrDefault();
        return vorige ?? snapTargets[0];
    }

    public static IReadOnlyList<double> BepaalBezetteGatenVanBoven(Kast kast)
    {
        var gaatjes = BepaalGaatjesMetPlankHoogte(kast);
        if (gaatjes.Count == 0 || kast.Planken.Count == 0)
            return [];

        return kast.Planken
            .Select(plank =>
            {
                var match = gaatjes.MinBy(item => Math.Abs(item.HoogteVanBodem - plank.HoogteVanBodem));
                return Math.Abs(match.HoogteVanBodem - plank.HoogteVanBodem) <= TolerantieMm
                    ? (double?)match.GatVanBoven
                    : null;
            })
            .Where(hoogte => hoogte.HasValue)
            .Select(hoogte => hoogte!.Value)
            .Distinct()
            .ToList();
    }

    public static double BepaalHoogteVanBoven(Kast kast, double hoogteVanBodem)
        => Math.Round((kast.Hoogte - kast.Wanddikte) - hoogteVanBodem, 1);

    private static List<(double GatVanBoven, double HoogteVanBodem)> BepaalGaatjesMetPlankHoogte(Kast kast)
    {
        var (minimum, maximum) = BepaalPlankBereik(kast);

        return ScharnierBerekeningService.GaatjesRijPosities(
                kast.Hoogte,
                kast.EersteGaatPositieVanafBoven,
                kast.GaatjesAfstand)
            .Select(gatVanBoven => (
                GatVanBoven: Math.Round(gatVanBoven, 1),
                HoogteVanBodem: Math.Round((kast.Hoogte - kast.Wanddikte) - gatVanBoven, 1)))
            .Where(item => item.HoogteVanBodem >= minimum - TolerantieMm && item.HoogteVanBodem <= maximum + TolerantieMm)
            .OrderBy(item => item.HoogteVanBodem)
            .ToList();
    }

    private static (double Minimum, double Maximum) BepaalPlankBereik(Kast kast)
    {
        var minimum = Math.Max(0, kast.Wanddikte);
        var maximum = Math.Max(minimum, kast.Hoogte - kast.Wanddikte * 2);
        return (minimum, maximum);
    }
}
