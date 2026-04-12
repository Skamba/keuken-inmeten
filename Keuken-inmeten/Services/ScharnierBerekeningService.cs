namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class ScharnierBerekeningService
{
    private sealed record PaneelSegment(Kast Kast, double TopVanPaneel, double KastOffsetVanBoven, double Hoogte);
    private sealed record ScharnierKandidaat(
        Kast Kast,
        double PaneelY,
        double MontagePlaatMiddenInKast,
        int GaatBovenIndex,
        int GaatOnderIndex,
        double GaatBovenY,
        double GaatOnderY);

    public const double CupDiameter = 35.0;
    public const double CupCenterVanRand = KeukenDomeinDefaults.PaneelDefaults.PotHartVanRand;
    public const double MinCupCenterVanRand = CupDiameter / 2.0;
    public const double MinAfstandVanRand = 80.0;
}
