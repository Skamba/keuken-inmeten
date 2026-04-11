namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class KeukenDomeinValidatieService
{
    private static readonly KeukenWand DefaultWand = new();
    private static readonly Kast DefaultKast = new();
    private static readonly PaneelToewijzing DefaultToewijzing = new();
    private static readonly Kast DefaultTemplateKast = IndelingFormulierHelper.NieuweKast();

    public static KeukenData NormaliseerData(KeukenData data) => new()
    {
        Wanden = [.. data.Wanden.Select(NormaliseerWand)],
        Kasten = [.. data.Kasten.Select(NormaliseerKast)],
        Apparaten = [.. data.Apparaten.Select(NormaliseerApparaat)],
        Toewijzingen = [.. data.Toewijzingen.Select(NormaliseerToewijzing)],
        VerificatieStatussen = NormaliseerVerificatieStatussen(data),
        KastTemplates = [.. data.KastTemplates.Select(NormaliseerKastTemplate)],
        LaatstGebruiktePotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(data.LaatstGebruiktePotHartVanRand),
        PaneelRandSpeling = PaneelSpelingService.NormaliseerRandSpeling(data.PaneelRandSpeling)
    };

    public static KeukenWand NormaliseerWand(KeukenWand wand) => new()
    {
        Id = wand.Id,
        Naam = NormaliseerNaam(wand.Naam),
        Breedte = NormaliseerPositieveMaat(wand.Breedte, DefaultWand.Breedte),
        Hoogte = NormaliseerPositieveMaat(wand.Hoogte, DefaultWand.Hoogte),
        PlintHoogte = NormaliseerNietNegatieveMaat(wand.PlintHoogte, DefaultWand.PlintHoogte),
        KastIds = NormaliseerIdLijst(wand.KastIds),
        ApparaatIds = NormaliseerIdLijst(wand.ApparaatIds)
    };

    public static Kast NormaliseerKast(Kast kast)
    {
        var genormaliseerd = new Kast
        {
            Id = kast.Id,
            Naam = NormaliseerNaam(kast.Naam),
            Type = NormaliseerEnum(kast.Type, DefaultKast.Type),
            Breedte = NormaliseerPositieveMaat(kast.Breedte, DefaultKast.Breedte),
            Hoogte = NormaliseerPositieveMaat(kast.Hoogte, DefaultKast.Hoogte),
            Diepte = NormaliseerPositieveMaat(kast.Diepte, DefaultKast.Diepte),
            Wanddikte = NormaliseerPositieveMaat(kast.Wanddikte, DefaultKast.Wanddikte),
            GaatjesAfstand = NormaliseerPositieveMaat(kast.GaatjesAfstand, DefaultKast.GaatjesAfstand),
            EersteGaatVanBoven = NormaliseerPositieveMaat(kast.EersteGaatVanBoven, DefaultKast.EersteGaatVanBoven),
            HoogteVanVloer = NormaliseerPositie(kast.HoogteVanVloer),
            XPositie = NormaliseerPositie(kast.XPositie)
        };

        genormaliseerd.MontagePlaatPosities = IndelingFormulierHelper.BerekenMontageplaatPosities(genormaliseerd);
        genormaliseerd.Planken =
        [
            .. kast.Planken.Select(plank => new Plank
            {
                Id = plank.Id,
                HoogteVanBodem = NormaliseerPlankHoogte(plank.HoogteVanBodem, genormaliseerd.Hoogte)
            })
        ];
        return genormaliseerd;
    }

    public static KastTemplate NormaliseerKastTemplate(KastTemplate template) => new()
    {
        Id = template.Id,
        Naam = NormaliseerNaam(template.Naam),
        Type = NormaliseerEnum(template.Type, DefaultTemplateKast.Type),
        Breedte = NormaliseerPositieveMaat(template.Breedte, DefaultTemplateKast.Breedte),
        Hoogte = NormaliseerPositieveMaat(template.Hoogte, DefaultTemplateKast.Hoogte),
        Diepte = NormaliseerPositieveMaat(template.Diepte, DefaultTemplateKast.Diepte),
        Wanddikte = NormaliseerPositieveMaat(template.Wanddikte, DefaultTemplateKast.Wanddikte),
        GaatjesAfstand = NormaliseerPositieveMaat(template.GaatjesAfstand, DefaultTemplateKast.GaatjesAfstand),
        EersteGaatVanBoven = NormaliseerPositieveMaat(template.EersteGaatVanBoven, DefaultTemplateKast.EersteGaatVanBoven),
        LaatstGebruikt = template.LaatstGebruikt
    };

    public static Apparaat NormaliseerApparaat(Apparaat apparaat)
    {
        var type = NormaliseerEnum(apparaat.Type, ApparaatType.Oven);
        var standaard = Apparaat.StandaardAfmetingen(type);

        return new Apparaat
        {
            Id = apparaat.Id,
            Naam = NormaliseerNaam(apparaat.Naam),
            Type = type,
            Breedte = NormaliseerPositieveMaat(apparaat.Breedte, standaard.breedte),
            Hoogte = NormaliseerPositieveMaat(apparaat.Hoogte, standaard.hoogte),
            Diepte = NormaliseerPositieveMaat(apparaat.Diepte, standaard.diepte),
            HoogteVanVloer = NormaliseerPositie(apparaat.HoogteVanVloer),
            XPositie = NormaliseerPositie(apparaat.XPositie)
        };
    }

    public static PaneelToewijzing NormaliseerToewijzing(PaneelToewijzing toewijzing)
    {
        var type = NormaliseerEnum(toewijzing.Type, DefaultToewijzing.Type);
        return new PaneelToewijzing
        {
            Id = toewijzing.Id,
            KastIds = NormaliseerIdLijst(toewijzing.KastIds),
            Type = type,
            ScharnierZijde = NormaliseerEnum(toewijzing.ScharnierZijde, DefaultToewijzing.ScharnierZijde),
            PotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(toewijzing.PotHartVanRand),
            Breedte = NormaliseerPositieveMaat(toewijzing.Breedte, 1),
            Hoogte = NormaliseerPositieveMaat(toewijzing.Hoogte, 1),
            XPositie = toewijzing.XPositie is null ? null : NormaliseerPositie(toewijzing.XPositie.Value),
            HoogteVanVloer = toewijzing.HoogteVanVloer is null ? null : NormaliseerPositie(toewijzing.HoogteVanVloer.Value)
        };
    }

    public static PaneelVerificatieStatus NormaliseerVerificatieStatus(PaneelVerificatieStatus status) => new()
    {
        ToewijzingId = status.ToewijzingId,
        MatenOk = status.MatenOk,
        ScharnierPositiesOk = status.ScharnierPositiesOk
    };

    public static double NormaliseerPositie(double waarde)
        => NormaliseerNietNegatieveMaat(waarde, 0);

    public static double NormaliseerPlankHoogte(double hoogteVanBodem, double kastHoogte)
        => Math.Round(Math.Clamp(NormaliseerPositie(hoogteVanBodem), 0, Math.Max(0, kastHoogte)), 1);

    private static TEnum NormaliseerEnum<TEnum>(TEnum waarde, TEnum fallback)
        where TEnum : struct, Enum
        => Enum.IsDefined(typeof(TEnum), waarde) ? waarde : fallback;

    private static string NormaliseerNaam(string naam)
        => string.IsNullOrWhiteSpace(naam) ? string.Empty : naam.Trim();

    private static List<Guid> NormaliseerIdLijst(IEnumerable<Guid> ids)
        => [.. ids.Distinct()];

    private static List<PaneelVerificatieStatus> NormaliseerVerificatieStatussen(KeukenData data)
    {
        var geldigeToewijzingIds = data.Toewijzingen
            .Select(toewijzing => toewijzing.Id)
            .ToHashSet();

        return [..
            data.VerificatieStatussen
                .Where(status => geldigeToewijzingIds.Contains(status.ToewijzingId))
                .GroupBy(status => status.ToewijzingId)
                .Select(groep => NormaliseerVerificatieStatus(groep.Last()))];
    }

    private static double NormaliseerPositieveMaat(double waarde, double fallback)
    {
        if (double.IsNaN(waarde) || double.IsInfinity(waarde) || waarde <= 0)
            return fallback;

        return Math.Round(waarde, 1);
    }

    private static double NormaliseerNietNegatieveMaat(double waarde, double fallback)
    {
        if (double.IsNaN(waarde) || double.IsInfinity(waarde))
            return fallback;

        return Math.Round(Math.Max(0, waarde), 1);
    }
}
