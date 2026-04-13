namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class IndelingFormulierHelper
{
    public static bool TryVindVrijeKastPlaatsing(
        KeukenWand wand,
        IEnumerable<Kast> bestaandeKastenBron,
        Kast kast,
        out (double xPositie, double hoogteVanVloer) plaatsing,
        Guid? uitsluitenKastId = null)
    {
        if (!KastPlaatsingService.TryVindVrijePlaatsing(
                wand,
                bestaandeKastenBron,
                kast,
                out var gevondenPlaatsing,
                uitsluitenKastId))
        {
            plaatsing = default;
            return false;
        }

        plaatsing = (gevondenPlaatsing.XPositie, gevondenPlaatsing.HoogteVanVloer);
        return true;
    }

    public static bool IsVrijeKastPlaatsing(
        KeukenWand wand,
        IEnumerable<Kast> bestaandeKastenBron,
        Kast kast,
        double xPositie,
        double hoogteVanVloer,
        Guid? uitsluitenKastId = null)
        => KastPlaatsingService.IsVrijePlaatsing(
            wand,
            bestaandeKastenBron,
            kast,
            xPositie,
            hoogteVanVloer,
            uitsluitenKastId);
}
