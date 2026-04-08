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
}
