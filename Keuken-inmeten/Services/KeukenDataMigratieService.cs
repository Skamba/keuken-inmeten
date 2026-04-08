namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class KeukenDataMigratieService
{
    public const int LegacySchemaVersie = 1;
    public const int HuidigeSchemaVersie = 2;

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
            LegacySchemaVersie => TryMaakMigratieResultaat(data, behoudTemplates, out resultaat),
            HuidigeSchemaVersie => TryMaakMigratieResultaat(data, behoudTemplates, out resultaat),
            _ => false
        };
    }

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
