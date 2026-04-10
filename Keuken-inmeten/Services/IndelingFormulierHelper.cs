namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class IndelingFormulierHelper
{
    public static Kast NieuweKast() => KeukenDomeinDefaults.NieuweKast();

    public static KeukenWand NieuweWand() => KeukenDomeinDefaults.NieuweWand();

    public static Apparaat NieuwApparaat(ApparaatType type = ApparaatType.Oven)
    {
        var (breedte, hoogte, diepte) = Apparaat.StandaardAfmetingen(type);
        return new Apparaat
        {
            Naam = "",
            Type = type,
            Breedte = breedte,
            Hoogte = hoogte,
            Diepte = diepte
        };
    }

    public static PaneelToewijzing NieuwePaneelToewijzing(double potHartVanRand = ScharnierBerekeningService.CupCenterVanRand)
        => new()
        {
            PotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(potHartVanRand)
        };

    public static Kast MaakKastVanTemplate(KastTemplate template) => new()
    {
        Naam = template.Naam,
        Type = InferKastType(template.Hoogte),
        Breedte = template.Breedte,
        Hoogte = template.Hoogte,
        Diepte = template.Diepte,
        Wanddikte = template.Wanddikte,
        GaatjesAfstand = template.GaatjesAfstand,
        EersteGaatVanBoven = template.EersteGaatVanBoven
    };

    public static Kast MaakKastMetVorigeWaarden(IEnumerable<KastTemplate> templates)
    {
        var laatsteTemplate = templates
            .OrderByDescending(template => template.LaatstGebruikt)
            .FirstOrDefault();

        if (laatsteTemplate is null)
            return NieuweKast();

        var kast = MaakKastVanTemplate(laatsteTemplate);
        kast.Naam = "";
        return kast;
    }

    public static KastType InferKastType(double hoogte) => hoogte switch
    {
        <= 500 => KastType.Bovenkast,
        >= 1100 => KastType.HogeKast,
        _ => KastType.Onderkast
    };

    public static bool HeeftAfwijkendeTechnischeInstellingen(Kast kast)
        => Math.Abs(kast.Wanddikte - KeukenDomeinDefaults.KastDefaults.Wanddikte) > 0.001
            || Math.Abs(kast.GaatjesAfstand - KeukenDomeinDefaults.KastDefaults.GaatjesAfstand) > 0.001
            || Math.Abs(kast.EersteGaatVanBoven - KeukenDomeinDefaults.KastDefaults.EersteGaatVanBoven) > 0.001
            || kast.Planken.Count > 0;

    public static List<MontagePlaatPositie> BerekenMontageplaatPosities(Kast kast)
        => BerekenMontageplaatPosities(kast.Hoogte, kast.EersteGaatPositieVanafBoven, kast.GaatjesAfstand);

    public static List<MontagePlaatPositie> BerekenMontageplaatPosities(
        double hoogte,
        double eersteGaatPositieVanafBoven,
        double gaatjesAfstand)
        => ScharnierBerekeningService.BerekenStandaardPosities(hoogte, eersteGaatPositieVanafBoven, gaatjesAfstand)
            .SelectMany(afstand => new[]
            {
                new MontagePlaatPositie { AfstandVanBoven = afstand, Zijde = ScharnierZijde.Links },
                new MontagePlaatPositie { AfstandVanBoven = afstand, Zijde = ScharnierZijde.Rechts }
            })
            .OrderBy(positie => positie.AfstandVanBoven)
            .ThenBy(positie => positie.Zijde)
            .ToList();

    public static Kast KopieerKast(Kast bron, bool behoudPlankIds = true) => new()
    {
        Id = bron.Id,
        Naam = bron.Naam,
        Type = bron.Type,
        Breedte = bron.Breedte,
        Hoogte = bron.Hoogte,
        Diepte = bron.Diepte,
        Wanddikte = bron.Wanddikte,
        GaatjesAfstand = bron.GaatjesAfstand,
        EersteGaatVanBoven = bron.EersteGaatVanBoven,
        HoogteVanVloer = bron.HoogteVanVloer,
        XPositie = bron.XPositie,
        MontagePlaatPosities = bron.MontagePlaatPosities
            .Select(positie => new MontagePlaatPositie
            {
                AfstandVanBoven = positie.AfstandVanBoven,
                Zijde = positie.Zijde
            })
            .ToList(),
        Planken = bron.Planken
            .Select(plank => new Plank
            {
                Id = behoudPlankIds ? plank.Id : Guid.NewGuid(),
                HoogteVanBodem = plank.HoogteVanBodem
            })
            .ToList()
    };

    public static bool TryVindVrijeKastPlaatsing(
        KeukenWand wand,
        IEnumerable<Kast> bestaandeKastenBron,
        Kast kast,
        out (double xPositie, double hoogteVanVloer) plaatsing,
        Guid? uitsluitenKastId = null)
    {
        var bestaandeKasten = bestaandeKastenBron
            .Where(bestaandeKast => bestaandeKast.Id != uitsluitenKastId)
            .ToList();
        var maxX = wand.Breedte - kast.Breedte;
        var maxY = wand.Hoogte - kast.Hoogte;

        if (maxX < -0.001 || maxY < -0.001)
        {
            plaatsing = default;
            return false;
        }

        var kandidaatY = Math.Clamp(KeukenDomeinValidatieService.NormaliseerPositie(kast.HoogteVanVloer), 0, Math.Max(0, maxY));
        foreach (var kandidaatX in BepaalKastPlaatsKandidaten(kast, bestaandeKasten, maxX))
        {
            if (!IsVrijeKastPlaats(kandidaatX, kandidaatY, kast, bestaandeKasten))
                continue;

            plaatsing = (Math.Round(kandidaatX, 1), Math.Round(kandidaatY, 1));
            return true;
        }

        plaatsing = default;
        return false;
    }

    public static Apparaat KopieerApparaat(Apparaat bron) => new()
    {
        Id = bron.Id,
        Naam = bron.Naam,
        Type = bron.Type,
        Breedte = bron.Breedte,
        Hoogte = bron.Hoogte,
        Diepte = bron.Diepte,
        HoogteVanVloer = bron.HoogteVanVloer,
        XPositie = bron.XPositie
    };

    public static PaneelToewijzing KopieerToewijzing(PaneelToewijzing bron) => new()
    {
        Id = bron.Id,
        KastIds = [.. bron.KastIds],
        Type = bron.Type,
        ScharnierZijde = bron.ScharnierZijde,
        PotHartVanRand = bron.PotHartVanRand,
        Breedte = bron.Breedte,
        Hoogte = bron.Hoogte,
        XPositie = bron.XPositie,
        HoogteVanVloer = bron.HoogteVanVloer
    };

    public static string ApparaatTypeLabel(ApparaatType type) => type switch
    {
        ApparaatType.Oven => "Oven",
        ApparaatType.Magnetron => "Magnetron",
        ApparaatType.Vaatwasser => "Vaatwasser",
        ApparaatType.Koelkast => "Koelkast",
        ApparaatType.Vriezer => "Vriezer",
        ApparaatType.Kookplaat => "Kookplaat",
        ApparaatType.Afzuigkap => "Afzuigkap",
        _ => type.ToString()
    };

    private static IEnumerable<double> BepaalKastPlaatsKandidaten(Kast kast, IReadOnlyList<Kast> bestaandeKasten, double maxX)
    {
        var gezien = new HashSet<double>();

        if (VoegKandidaatToe(Math.Clamp(KeukenDomeinValidatieService.NormaliseerPositie(kast.XPositie), 0, Math.Max(0, maxX))))
            yield return Math.Clamp(KeukenDomeinValidatieService.NormaliseerPositie(kast.XPositie), 0, Math.Max(0, maxX));

        if (VoegKandidaatToe(0))
            yield return 0;

        foreach (var kandidaat in bestaandeKasten
                     .OrderBy(bestaandeKast => bestaandeKast.XPositie)
                     .Select(bestaandeKast => Math.Clamp(Math.Round(bestaandeKast.XPositie + bestaandeKast.Breedte, 1), 0, Math.Max(0, maxX))))
        {
            if (VoegKandidaatToe(kandidaat))
                yield return kandidaat;
        }

        bool VoegKandidaatToe(double kandidaat)
            => gezien.Add(Math.Round(kandidaat, 1));
    }

    private static bool IsVrijeKastPlaats(double xPositie, double hoogteVanVloer, Kast kast, IReadOnlyList<Kast> bestaandeKasten)
        => bestaandeKasten.All(bestaandeKast =>
            !HeeftOverlap(
                xPositie,
                hoogteVanVloer,
                kast.Breedte,
                kast.Hoogte,
                bestaandeKast.XPositie,
                bestaandeKast.HoogteVanVloer,
                bestaandeKast.Breedte,
                bestaandeKast.Hoogte));

    private static bool HeeftOverlap(
        double linksX,
        double linksY,
        double linksBreedte,
        double linksHoogte,
        double rechtsX,
        double rechtsY,
        double rechtsBreedte,
        double rechtsHoogte)
    {
        var overlapX = Math.Min(linksX + linksBreedte, rechtsX + rechtsBreedte) - Math.Max(linksX, rechtsX);
        var overlapY = Math.Min(linksY + linksHoogte, rechtsY + rechtsHoogte) - Math.Max(linksY, rechtsY);
        return overlapX > 0.1 && overlapY > 0.1;
    }
}
