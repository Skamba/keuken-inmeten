using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class KeukenDataMigratieServiceTests
{
    [Fact]
    public void Lokale_opslag_migratie_bewaart_templates_en_normaliseert_waarden()
    {
        var legacy = MaakVoorbeeldData();

        var migreerde = KeukenDataMigratieService.TryMigreerLokaleOpslag(
            KeukenDataMigratieService.LegacySchemaVersie,
            legacy,
            out var gemigreerd);

        Assert.True(migreerde);
        Assert.Single(gemigreerd.KastTemplates);
        Assert.Equal(ScharnierBerekeningService.MinCupCenterVanRand, gemigreerd.LaatstGebruiktePotHartVanRand);
        Assert.Equal(0, gemigreerd.PaneelRandSpeling);
    }

    [Fact]
    public void Deeldata_migratie_verwijdert_templates_en_normaliseert_waarden()
    {
        var legacy = MaakVoorbeeldData();

        var migreerde = KeukenDataMigratieService.TryMigreerDeelData(
            KeukenDataMigratieService.LegacySchemaVersie,
            legacy,
            out var gemigreerd);

        Assert.True(migreerde);
        Assert.Empty(gemigreerd.KastTemplates);
        Assert.Equal(ScharnierBerekeningService.MinCupCenterVanRand, gemigreerd.LaatstGebruiktePotHartVanRand);
        Assert.Equal(0, gemigreerd.PaneelRandSpeling);
    }

    [Fact]
    public void Onbekende_schema_versie_wordt_afgewezen()
    {
        var gemigreerde = KeukenDataMigratieService.TryMigreerLokaleOpslag(99, MaakVoorbeeldData(), out var gemigreerd);

        Assert.False(gemigreerde);
        Assert.Empty(gemigreerd.Wanden);
        Assert.Empty(gemigreerd.Kasten);
    }

    [Fact]
    public void Schema2_migratie_zet_per_paneel_speling_om_naar_totale_voeg_als_er_panelen_bestaan()
    {
        var legacy = MaakVoorbeeldData();
        legacy.PaneelRandSpeling = 1.5;
        legacy.Toewijzingen =
        [
            new PaneelToewijzing
            {
                Id = Guid.NewGuid(),
                KastIds = [legacy.Kasten[0].Id],
                Type = PaneelType.Deur,
                Breedte = 600,
                Hoogte = 720
            }
        ];

        var migreerde = KeukenDataMigratieService.TryMigreerLokaleOpslag(
            KeukenDataMigratieService.PerPaneelSpelingSchemaVersie,
            legacy,
            out var gemigreerd);

        Assert.True(migreerde);
        Assert.Equal(3, gemigreerd.PaneelRandSpeling);
    }

    [Fact]
    public void Schema2_migratie_zet_de_oude_standaard_zonder_panelen_om_naar_de_nieuwe_default()
    {
        var legacy = MaakVoorbeeldData();
        legacy.PaneelRandSpeling = PaneelSpelingService.LegacyDefaultRandSpeling;

        var migreerde = KeukenDataMigratieService.TryMigreerLokaleOpslag(
            KeukenDataMigratieService.PerPaneelSpelingSchemaVersie,
            legacy,
            out var gemigreerd);

        Assert.True(migreerde);
        Assert.Equal(PaneelSpelingService.DefaultRandSpeling, gemigreerd.PaneelRandSpeling);
    }

    private static KeukenData MaakVoorbeeldData()
    {
        var wandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var kastId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        return new KeukenData
        {
            LaatstGebruiktePotHartVanRand = 10,
            PaneelRandSpeling = -1,
            Wanden =
            [
                new KeukenWand
                {
                    Id = wandId,
                    Naam = "Achterwand",
                    KastIds = [kastId]
                }
            ],
            Kasten =
            [
                new Kast
                {
                    Id = kastId,
                    Naam = "Onderkast",
                    Type = KastType.Onderkast,
                    Breedte = 600,
                    Hoogte = 720
                }
            ],
            KastTemplates =
            [
                new KastTemplate
                {
                    Naam = "Template",
                    Type = KastType.Onderkast,
                    Breedte = 600,
                    Hoogte = 720,
                    Diepte = 560,
                    Wanddikte = 18,
                    GaatjesAfstand = 32,
                    EersteGaatVanBoven = 19,
                    LaatstGebruikt = new DateTime(2026, 4, 8, 10, 0, 0, DateTimeKind.Utc)
                }
            ]
        };
    }
}
