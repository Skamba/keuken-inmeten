namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class PaneelLayoutService
{
    public const double MinPaneelMaat = 50.0;

    public static PaneelRechthoek? BerekenRechthoek(PaneelToewijzing toewijzing, IEnumerable<Kast> kasten)
    {
        if (toewijzing.Breedte <= 0 || toewijzing.Hoogte <= 0)
            return null;

        if (toewijzing.XPositie is double xPositie && toewijzing.HoogteVanVloer is double hoogteVanVloer)
        {
            return new PaneelRechthoek
            {
                XPositie = xPositie,
                HoogteVanVloer = hoogteVanVloer,
                Breedte = toewijzing.Breedte,
                Hoogte = toewijzing.Hoogte
            };
        }

        return BerekenOmhullende(kasten);
    }

    public static PaneelRechthoek? BerekenOmhullende(IEnumerable<Kast> kastenBron)
    {
        var kasten = kastenBron.ToList();
        if (kasten.Count == 0)
            return null;

        var minX = kasten.Min(k => k.XPositie);
        var maxX = kasten.Max(k => k.XPositie + k.Breedte);
        var minVloer = kasten.Min(k => k.HoogteVanVloer);
        var maxTop = kasten.Max(k => k.HoogteVanVloer + k.Hoogte);

        return new PaneelRechthoek
        {
            XPositie = minX,
            HoogteVanVloer = minVloer,
            Breedte = maxX - minX,
            Hoogte = maxTop - minVloer
        };
    }

    public static List<Kast> BepaalOverlappendeKasten(IEnumerable<Kast> kastenBron, PaneelRechthoek paneel)
        => kastenBron
            .Where(kast => HeeftOverlap(paneel, kast))
            .OrderBy(kast => kast.XPositie)
            .ThenByDescending(kast => kast.HoogteVanVloer)
            .ThenBy(kast => kast.Naam)
            .ToList();

    public static bool HeeftOverlap(PaneelRechthoek paneel, Kast kast, double minimumOverlap = 1.0)
    {
        var overlapX = Math.Min(paneel.Rechterkant, kast.XPositie + kast.Breedte) - Math.Max(paneel.XPositie, kast.XPositie);
        var overlapY = Math.Min(paneel.Bovenzijde, kast.HoogteVanVloer + kast.Hoogte) - Math.Max(paneel.HoogteVanVloer, kast.HoogteVanVloer);
        return overlapX >= minimumOverlap && overlapY >= minimumOverlap;
    }

    public static PaneelRechthoek ClampBinnen(PaneelRechthoek paneel, PaneelRechthoek grenzen)
    {
        var maxBreedte = Math.Max(0, grenzen.Breedte);
        var maxHoogte = Math.Max(0, grenzen.Hoogte);
        var minBreedte = Math.Min(MinPaneelMaat, maxBreedte);
        var minHoogte = Math.Min(MinPaneelMaat, maxHoogte);

        var breedte = Math.Clamp(paneel.Breedte, minBreedte, maxBreedte);
        var hoogte = Math.Clamp(paneel.Hoogte, minHoogte, maxHoogte);
        var xPositie = Math.Clamp(paneel.XPositie, grenzen.XPositie, grenzen.Rechterkant - breedte);
        var hoogteVanVloer = Math.Clamp(paneel.HoogteVanVloer, grenzen.HoogteVanVloer, grenzen.Bovenzijde - hoogte);

        return new PaneelRechthoek
        {
            XPositie = xPositie,
            HoogteVanVloer = hoogteVanVloer,
            Breedte = breedte,
            Hoogte = hoogte
        };
    }
}
