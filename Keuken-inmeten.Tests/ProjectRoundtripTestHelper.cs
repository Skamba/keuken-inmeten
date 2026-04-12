using System.Text.Json;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

internal static class ProjectRoundtripTestHelper
{
    private static readonly JsonSerializerOptions JsonOpties = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static KeukenData MaakVolledigProjectSnapshot()
    {
        var wandAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var wandBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var kastOnderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var kastHoogId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var apparaatId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var deurToewijzingId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var blindToewijzingId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var plankId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var templateId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var ruweData = new KeukenData
        {
            LaatstGebruiktePotHartVanRand = 24.5,
            PaneelRandSpeling = 5,
            Wanden =
            [
                new KeukenWand
                {
                    Id = wandAId,
                    Naam = "Achterwand",
                    Breedte = 3600,
                    Hoogte = 2750,
                    PlintHoogte = 120,
                    KastIds = [kastOnderId, kastHoogId],
                    ApparaatIds = [apparaatId]
                },
                new KeukenWand
                {
                    Id = wandBId,
                    Naam = "Linkerwand",
                    Breedte = 2400,
                    Hoogte = 2500,
                    PlintHoogte = 90
                }
            ],
            Kasten =
            [
                new Kast
                {
                    Id = kastOnderId,
                    Naam = "Onderkast links",
                    Type = KastType.Onderkast,
                    Breedte = 800,
                    Hoogte = 720,
                    Diepte = 580,
                    Wanddikte = 19,
                    GaatjesAfstand = 28,
                    EersteGaatVanBoven = 21,
                    XPositie = 50,
                    HoogteVanVloer = 110,
                    Planken =
                    [
                        new Plank
                        {
                            Id = plankId,
                            HoogteVanBodem = 355
                        }
                    ]
                },
                new Kast
                {
                    Id = kastHoogId,
                    Naam = "Hoge kast",
                    Type = KastType.HogeKast,
                    Breedte = 650,
                    Hoogte = 2300,
                    Diepte = 610,
                    Wanddikte = 20,
                    GaatjesAfstand = 30,
                    EersteGaatVanBoven = 18,
                    XPositie = 1800,
                    HoogteVanVloer = 0
                }
            ],
            Apparaten =
            [
                new Apparaat
                {
                    Id = apparaatId,
                    Naam = "Magnetron",
                    Type = ApparaatType.Magnetron,
                    Breedte = 590,
                    Hoogte = 380,
                    Diepte = 410,
                    XPositie = 900,
                    HoogteVanVloer = 1450
                }
            ],
            Toewijzingen =
            [
                new PaneelToewijzing
                {
                    Id = deurToewijzingId,
                    KastIds = [kastHoogId],
                    Type = PaneelType.Deur,
                    ScharnierZijde = ScharnierZijde.Rechts,
                    PotHartVanRand = 24.5,
                    Breedte = 620,
                    Hoogte = 2100,
                    XPositie = 1815,
                    HoogteVanVloer = 100
                },
                new PaneelToewijzing
                {
                    Id = blindToewijzingId,
                    KastIds = [kastOnderId],
                    Type = PaneelType.BlindPaneel,
                    ScharnierZijde = ScharnierZijde.Links,
                    PotHartVanRand = 23.5,
                    Breedte = 780,
                    Hoogte = 300,
                    XPositie = 60,
                    HoogteVanVloer = 640
                }
            ],
            VerificatieStatussen =
            [
                new PaneelVerificatieStatus
                {
                    ToewijzingId = deurToewijzingId,
                    MatenOk = true,
                    ScharnierPositiesOk = true
                },
                new PaneelVerificatieStatus
                {
                    ToewijzingId = blindToewijzingId,
                    MatenOk = true,
                    ScharnierPositiesOk = false
                }
            ],
            KastTemplates =
            [
                new KastTemplate
                {
                    Id = templateId,
                    Naam = "Favoriete hoge kast",
                    Type = KastType.HogeKast,
                    Breedte = 650,
                    Hoogte = 2300,
                    Diepte = 610,
                    Wanddikte = 20,
                    GaatjesAfstand = 30,
                    EersteGaatVanBoven = 18,
                    LaatstGebruikt = new DateTime(2026, 4, 12, 8, 30, 0, DateTimeKind.Utc)
                }
            ]
        };

        var state = new KeukenStateService();
        state.Laden(ruweData);
        return state.Exporteren();
    }

    public static void AssertZelfdeProject(KeukenData verwacht, KeukenData actueel)
        => Assert.Equal(Serialiseer(verwacht), Serialiseer(actueel));

    private static string Serialiseer(KeukenData data)
        => JsonSerializer.Serialize(data, JsonOpties);
}
