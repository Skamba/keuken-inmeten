namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class KeukenDataMigratieService
{
    public const int LegacySchemaVersie = 1;
    public const int PerPaneelSpelingSchemaVersie = 2;
    public const int HuidigeSchemaVersie = 3;

    public static KeukenData MaakHuidigeLokaleOpslagData(KeukenData data)
        => MaakGenormaliseerdeKopie(data, behoudTemplates: true);

    public static KeukenData MaakHuidigeDeelData(KeukenData data)
        => MaakGenormaliseerdeKopie(data, behoudTemplates: false);

    public static bool TryMigreerLokaleOpslag(int schemaVersie, KeukenData? data, out KeukenData resultaat)
        => TryMigreer(schemaVersie, data, behoudTemplates: true, out resultaat);

    public static bool TryMigreerDeelData(int schemaVersie, KeukenData? data, out KeukenData resultaat)
        => TryMigreer(schemaVersie, data, behoudTemplates: false, out resultaat);

    private static bool TryMigreer(int schemaVersie, KeukenData? data, bool behoudTemplates, out KeukenData resultaat)
    {
        resultaat = new KeukenData();
        if (data is null)
            return false;

        return schemaVersie switch
        {
            LegacySchemaVersie => TryMaakMigratieResultaat(MigreerLegacyPaneelSpeling(data), behoudTemplates, out resultaat),
            PerPaneelSpelingSchemaVersie => TryMaakMigratieResultaat(MigreerLegacyPaneelSpeling(data), behoudTemplates, out resultaat),
            HuidigeSchemaVersie => TryMaakMigratieResultaat(data, behoudTemplates, out resultaat),
            _ => false
        };
    }

    private static KeukenData MigreerLegacyPaneelSpeling(KeukenData data)
        => new()
        {
            Wanden = data.Wanden,
            Kasten = data.Kasten,
            Apparaten = data.Apparaten,
            Toewijzingen = data.Toewijzingen,
            VerificatieStatussen = data.VerificatieStatussen,
            KastTemplates = data.KastTemplates,
            LaatstGebruiktePotHartVanRand = data.LaatstGebruiktePotHartVanRand,
            PaneelRandSpeling = PaneelSpelingService.MigreerLegacyRandSpeling(
                data.PaneelRandSpeling,
                data.Toewijzingen.Count > 0)
        };

    private static bool TryMaakMigratieResultaat(KeukenData data, bool behoudTemplates, out KeukenData resultaat)
    {
        resultaat = MaakGenormaliseerdeKopie(data, behoudTemplates);
        return true;
    }

    private static KeukenData MaakGenormaliseerdeKopie(KeukenData data, bool behoudTemplates)
    {
        var genormaliseerd = KeukenDomeinValidatieService.NormaliseerData(data);
        if (!behoudTemplates)
            genormaliseerd.KastTemplates = [];

        return genormaliseerd;
    }
}
