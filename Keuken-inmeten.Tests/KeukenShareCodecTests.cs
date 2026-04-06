using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class KeukenShareCodecTests
{
    [Fact]
    public void Encode_en_decode_behouden_de_keukenconfiguratie()
    {
        var wandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var kastAId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var kastBId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var apparaatId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var toewijzingId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var data = new KeukenData
        {
            Wanden =
            [
                new KeukenWand
                {
                    Id = wandId,
                    Naam = "Muur",
                    Breedte = 3600,
                    Hoogte = 2700,
                    PlintHoogte = 120,
                    KastIds = [kastAId, kastBId],
                    ApparaatIds = [apparaatId]
                }
            ],
            Kasten =
            [
                new Kast
                {
                    Id = kastAId,
                    Naam = "Onderkast links",
                    Type = KastType.Onderkast,
                    Breedte = 600,
                    Hoogte = 720,
                    XPositie = 0,
                    HoogteVanVloer = 0,
                    Planken = [new Plank { HoogteVanBodem = 360 }]
                },
                new Kast
                {
                    Id = kastBId,
                    Naam = "Hoge kast",
                    Type = KastType.HogeKast,
                    Breedte = 600,
                    Hoogte = 2600,
                    XPositie = 2400,
                    HoogteVanVloer = 0,
                    MontagePlaatPosities =
                    [
                        new MontagePlaatPositie
                        {
                            AfstandVanBoven = 1280,
                            Zijde = ScharnierZijde.Rechts
                        }
                    ]
                }
            ],
            Apparaten =
            [
                new Apparaat
                {
                    Id = apparaatId,
                    Naam = "Oven",
                    Type = ApparaatType.Oven,
                    Breedte = 600,
                    Hoogte = 600,
                    XPositie = 600,
                    HoogteVanVloer = 120
                }
            ],
            Toewijzingen =
            [
                new PaneelToewijzing
                {
                    Id = toewijzingId,
                    KastIds = [kastBId],
                    Type = PaneelType.Deur,
                    ScharnierZijde = ScharnierZijde.Rechts,
                    Breedte = 600,
                    Hoogte = 900,
                    XPositie = 2400,
                    HoogteVanVloer = 700
                }
            ],
            KastTemplates =
            [
                new KastTemplate
                {
                    Naam = "Wordt niet gedeeld",
                    Type = KastType.Onderkast,
                    Breedte = 600,
                    Hoogte = 720,
                    Diepte = 560,
                    Wanddikte = 18,
                    GaatjesAfstand = 32,
                    EersteGaatVanBoven = 19,
                    LaatstGebruikt = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        var token = KeukenShareCodec.Encode(data);

        var decodedOk = KeukenShareCodec.TryDecode(token, out var decoded);

        Assert.True(decodedOk);
        var wand = Assert.Single(decoded.Wanden);
        var kasten = decoded.Kasten.OrderBy(k => k.Naam).ToList();
        var apparaat = Assert.Single(decoded.Apparaten);
        var toewijzing = Assert.Single(decoded.Toewijzingen);

        Assert.Equal("Muur", wand.Naam);
        Assert.Equal(new[] { kastAId, kastBId }, wand.KastIds);
        Assert.Equal("Hoge kast", kasten[0].Naam);
        Assert.Equal("Onderkast links", kasten[1].Naam);
        Assert.Equal(1280, kasten[0].MontagePlaatPosities[0].AfstandVanBoven);
        Assert.Equal(360, kasten[1].Planken[0].HoogteVanBodem);
        Assert.Equal("Oven", apparaat.Naam);
        Assert.Equal(2400, toewijzing.XPositie);
        Assert.Equal(700, toewijzing.HoogteVanVloer);
        Assert.Empty(decoded.KastTemplates);
    }

    [Fact]
    public void Ongeldige_token_wordt_afgewezen()
    {
        var decodedOk = KeukenShareCodec.TryDecode("v1.geen-geldige-data", out var decoded);

        Assert.False(decodedOk);
        Assert.Empty(decoded.Wanden);
        Assert.Empty(decoded.Kasten);
    }
}
