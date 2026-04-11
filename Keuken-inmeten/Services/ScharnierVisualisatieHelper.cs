namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class ScharnierVisualisatieHelper
{
    public const double OverigGatRadius = 2.4;
    public const double StandaardGebruiktGatRadius = 4.1;
    public const double MinGebruiktGatRadius = 1.6;
    public const double VrijeMiddenruimtePx = 0.7;

    public static double BerekenGebruiktGatRadius(double scale, BoorgatOnderbouwing? onderbouwing)
    {
        if (onderbouwing is null || scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale))
            return StandaardGebruiktGatRadius;

        var hartafstandPx = Math.Abs(onderbouwing.GaatOnderY - onderbouwing.GaatBovenY) * scale;
        if (hartafstandPx <= 0)
            return MinGebruiktGatRadius;

        var maximaleRadius = Math.Max(0, (hartafstandPx - VrijeMiddenruimtePx) / 2.0);
        return Math.Clamp(maximaleRadius, MinGebruiktGatRadius, StandaardGebruiktGatRadius);
    }
}
