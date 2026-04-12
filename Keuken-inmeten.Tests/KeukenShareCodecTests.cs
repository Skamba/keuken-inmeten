using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Keuken_inmeten.Tests;

public class KeukenShareCodecTests
{
    [Fact]
    public void Compacte_json_encode_en_decode_behouden_de_keukenconfiguratie()
    {
        var data = MaakVoorbeeldData();

        var json = KeukenShareCodec.EncodeCompactJson(data);

        var decodedOk = KeukenShareCodec.TryDecodeCompactJson(json, out var decoded);

        Assert.True(decodedOk);
        var wand = Assert.Single(decoded.Wanden);
        var kasten = decoded.Kasten.OrderBy(k => k.Naam).ToList();
        var apparaat = Assert.Single(decoded.Apparaten);
        var toewijzing = Assert.Single(decoded.Toewijzingen);
        var hogeKast = Assert.Single(decoded.Kasten, k => k.Naam == "Hoge kast");
        var onderkast = Assert.Single(decoded.Kasten, k => k.Naam == "Onderkast links");

        Assert.Equal("Muur", wand.Naam);
        Assert.Equal(2, wand.KastIds.Count);
        Assert.Contains(hogeKast.Id, wand.KastIds);
        Assert.Contains(onderkast.Id, wand.KastIds);
        Assert.Equal("Hoge kast", kasten[0].Naam);
        Assert.Equal("Onderkast links", kasten[1].Naam);
        Assert.Equal(85, hogeKast.MontagePlaatPosities[0].AfstandVanBoven);
        Assert.Equal(360, onderkast.Planken[0].HoogteVanBodem);
        Assert.Equal("Oven", apparaat.Naam);
        Assert.Equal(2400, toewijzing.XPositie);
        Assert.Equal(700, toewijzing.HoogteVanVloer);
        Assert.Equal(600, toewijzing.Breedte);
        Assert.Equal(900, toewijzing.Hoogte);
        Assert.Equal(24.5, toewijzing.PotHartVanRand);
        Assert.Equal([hogeKast.Id], toewijzing.KastIds);
        Assert.Equal(24.5, decoded.LaatstGebruiktePotHartVanRand);
        var verificatieStatus = Assert.Single(decoded.VerificatieStatussen);
        Assert.Equal(toewijzing.Id, verificatieStatus.ToewijzingId);
        Assert.True(verificatieStatus.MatenOk);
        Assert.True(verificatieStatus.ScharnierPositiesOk);
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

    [Fact]
    public void Onbekende_share_versie_wordt_afgewezen()
    {
        var decodedOk = KeukenShareCodec.TryDecode("v99.geen-geldige-data", out var decoded);

        Assert.False(decodedOk);
        Assert.Empty(decoded.Wanden);
        Assert.Empty(decoded.Kasten);
    }

    [Fact]
    public void Legacy_v1_link_blijft_decodeerbaar()
    {
        var data = MaakVoorbeeldData();
        var token = MaakLegacyToken(data);

        var decodedOk = KeukenShareCodec.TryDecode(token, out var decoded);

        Assert.True(decodedOk);
        Assert.Single(decoded.Wanden);
        Assert.Equal(2, decoded.Kasten.Count);
        Assert.Single(decoded.Apparaten);
        Assert.Single(decoded.Toewijzingen);
        Assert.Equal(24.5, decoded.LaatstGebruiktePotHartVanRand);
    }

    [Fact]
    public void V4_token_prefix_wordt_herkend_en_gestript()
    {
        var token = KeukenShareCodec.MaakV4Token("abc123");

        Assert.True(KeukenShareCodec.IsV4Token(token));
        Assert.Equal("abc123", KeukenShareCodec.LeesV4Payload(token));
    }

    [Fact]
    public void V3_token_is_korter_dan_legacy_v1_token()
    {
        var data = MaakVoorbeeldData();

        var v1 = MaakLegacyToken(data);
        var v3 = KeukenShareCodec.Encode(data);

        Assert.StartsWith("v3.", v3);
        Assert.True(v3.Length < v1.Length, $"v3 token should be shorter than v1. v3={v3.Length}, v1={v1.Length}");
    }

    [Fact]
    public void V3_deelurl_is_korter_dan_equivalente_v2_deelurl()
    {
        var data = MaakVoorbeeldData();

        var v2 = MaakOngecomprimeerdeCompacteToken(data);
        var v3 = KeukenShareCodec.Encode(data);
        var v2Url = $"https://example.test/kasten?share={v2}";
        var v3Url = $"https://example.test/kasten?s={v3}";

        Assert.True(v3Url.Length < v2Url.Length, $"v3 share URL should be shorter than equivalent v2 share URL. v3={v3Url.Length}, v2={v2Url.Length}");
    }

    [Fact]
    public void V2_token_zonder_expliciete_ovenpositie_decodeert_naar_zichtbare_vrije_plek()
    {
        const string token = "v2.eyJ3IjpbeyJuIjoiTXV1ciIsImsiOlswLDEsMiwzLDQsNSw2XSwiYSI6WzBdfSx7Im4iOiJLZXVrZW4gdmFuYWYgdmFhdHdhc3NlciJ9LHsibiI6IkxhZGVzIn1dLCJrIjpbeyJuIjoiR3Jvb3QgMSIsImgiOjE5MDAsInciOjE2LCJlIjozMCwieCI6MjQwMCwieSI6MTAwfSx7Im4iOiJLbGVpbiIsImgiOjMyMCwidyI6MTYsImUiOjMwLCJ4IjoyNDAwLCJ5IjoyMDEwfSx7Im4iOiJCbGluZCIsImgiOjI3MCwidyI6MTYsIngiOjI0MDAsInkiOjIzMzB9LHsibiI6Ikdyb290IDIiLCJoIjoxOTAwLCJ3IjoxNiwiZSI6MzAsInkiOjEwMH0seyJuIjoiS2xlaW4gMiIsImgiOjMyMCwidyI6MTYsImUiOjMwLCJ4IjoxODAwLCJ5IjoyMDEwfSx7Im4iOiJCbGluZCAyIiwiaCI6MjcwLCJ3IjoxNiwieCI6MTgwMCwieSI6MjMzMH0seyJuIjoiTWVkaXVtIDEgIiwiaCI6NjQ1LCJ3IjoxNiwiZSI6NjIsIngiOjAsInkiOjEwMH1dLCJhIjpbeyJuIjoiT3ZlbiJ9XSwidCI6W3siYyI6WzEsMF0sInMiOjEsImIiOjYwMCwiaCI6MjIyMCwieCI6MjQwMCwieSI6MTAwfSx7ImMiOls0LDNdLCJzIjoxLCJiIjo2MDAsImgiOjIyMjAsIngiOjE4MDAsInkiOjEwMH0seyJjIjpbMl0sInQiOjJ9LHsiYyI6WzVdLCJ0IjoyfSx7ImMiOls2XX1dfQ";

        var decodedOk = KeukenShareCodec.TryDecode(token, out var decoded);

        Assert.True(decodedOk);

        var wand = Assert.Single(decoded.Wanden, item => item.Naam == "Muur");
        var oven = Assert.Single(decoded.Apparaten, item => item.Naam == "Oven");
        var wandKasten = decoded.Kasten
            .Where(kast => wand.KastIds.Contains(kast.Id))
            .ToList();

        Assert.Equal(600, oven.XPositie);
        Assert.Equal(100, oven.HoogteVanVloer);
        Assert.InRange(oven.XPositie, 0, wand.Breedte - oven.Breedte);
        Assert.InRange(oven.HoogteVanVloer, 0, wand.Hoogte - oven.Hoogte);
        Assert.DoesNotContain(wandKasten, kast => ApparaatLayoutService.HeeftOverlap(oven, kast));
    }

    [Fact]
    public void V3_roundtript_niet_standaard_paneelrandspeling()
    {
        var data = MaakVoorbeeldData();
        data.PaneelRandSpeling = 5;

        var token = KeukenShareCodec.Encode(data);
        var decodedOk = KeukenShareCodec.TryDecode(token, out var decoded);

        Assert.True(decodedOk);
        Assert.Equal(5, decoded.PaneelRandSpeling);
        var verificatieStatus = Assert.Single(decoded.VerificatieStatussen);
        Assert.True(verificatieStatus.MatenOk);
        Assert.True(verificatieStatus.ScharnierPositiesOk);
    }

    [Fact]
    public void Legacy_compacte_json_zonder_schema_migreert_per_paneel_speling_naar_totale_voeg()
    {
        const string json = """
            {"w":[{"n":"Muur","k":[0]}],"k":[{"n":"Onderkast","b":600,"h":720}],"t":[{"c":[0],"b":600,"h":720}],"r":2}
            """;

        var decodedOk = KeukenShareCodec.TryDecodeCompactJson(json, out var decoded);

        Assert.True(decodedOk);
        Assert.Equal(4, decoded.PaneelRandSpeling);
    }

    [Fact]
    public void V4_json_roundtript_alle_projectvariabelen_voor_deellinks()
    {
        var data = ProjectRoundtripTestHelper.MaakVolledigProjectSnapshot();

        var json = KeukenShareCodec.EncodeV4Json(data);
        var decodedOk = KeukenShareCodec.TryDecodeV4Json(json, out var decoded);

        Assert.True(decodedOk);
        ProjectRoundtripTestHelper.AssertZelfdeProject(data, decoded);
    }

    [Fact]
    public void V4_json_decoder_blijft_compatibel_met_bestaande_compacte_payloads()
    {
        var data = MaakVoorbeeldData();
        var compactJson = KeukenShareCodec.EncodeCompactJson(data);

        var decodedOk = KeukenShareCodec.TryDecodeV4Json(compactJson, out var decoded);

        Assert.True(decodedOk);
        Assert.Equal("Muur", Assert.Single(decoded.Wanden).Naam);
        Assert.Empty(decoded.KastTemplates);
    }

    private static KeukenData MaakVoorbeeldData()
    {
        var wandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var kastAId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var kastBId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var apparaatId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var toewijzingId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        return new KeukenData
        {
            LaatstGebruiktePotHartVanRand = 24.5,
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
                    PotHartVanRand = 24.5,
                    Breedte = 600,
                    Hoogte = 900,
                    XPositie = 2400,
                    HoogteVanVloer = 700
                }
            ],
            VerificatieStatussen =
            [
                new PaneelVerificatieStatus
                {
                    ToewijzingId = toewijzingId,
                    MatenOk = true,
                    ScharnierPositiesOk = true
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
    }

    private static string MaakLegacyToken(KeukenData data)
    {
        var deelbareData = new KeukenData
        {
            Wanden = [.. data.Wanden],
            Kasten = [.. data.Kasten],
            Apparaten = [.. data.Apparaten],
            Toewijzingen = [.. data.Toewijzingen],
            KastTemplates = [],
            LaatstGebruiktePotHartVanRand = data.LaatstGebruiktePotHartVanRand
        };
        var json = JsonSerializer.SerializeToUtf8Bytes(deelbareData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
        var token = Convert.ToBase64String(json)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return "v1." + token;
    }

    private static string MaakOngecomprimeerdeCompacteToken(KeukenData data)
    {
        var json = Encoding.UTF8.GetBytes(KeukenShareCodec.EncodeCompactJson(data));
        var token = Convert.ToBase64String(json)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return "v2." + token;
    }
}
