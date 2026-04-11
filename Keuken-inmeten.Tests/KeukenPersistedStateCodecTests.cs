using System.Text.Json;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class KeukenPersistedStateCodecTests
{
    private static readonly JsonSerializerOptions JsonOpties = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    [Fact]
    public void Encode_voegt_schema_versie_toe_en_roundtript_lokale_data()
    {
        var data = MaakVoorbeeldData();

        var json = KeukenPersistedStateCodec.Encode(data);
        var decodedOk = KeukenPersistedStateCodec.TryDecode(json, out var decoded);

        Assert.Contains("\"schemaVersion\":3", json);
        Assert.True(decodedOk);
        Assert.Single(decoded.KastTemplates);
        Assert.Equal("Template", decoded.KastTemplates[0].Naam);
    }

    [Fact]
    public void Legacy_localstorage_json_wordt_gemigreerd()
    {
        var legacyJson = JsonSerializer.Serialize(MaakVoorbeeldData(), JsonOpties);

        var decodedOk = KeukenPersistedStateCodec.TryDecode(legacyJson, out var decoded);

        Assert.True(decodedOk);
        Assert.Single(decoded.KastTemplates);
        Assert.Equal(ScharnierBerekeningService.MinCupCenterVanRand, decoded.LaatstGebruiktePotHartVanRand);
        Assert.Equal(0, decoded.PaneelRandSpeling);
    }

    [Fact]
    public void Onbekende_schema_versie_wordt_afgewezen()
    {
        const string json = """
            {"schemaVersion":99,"data":{"wanden":[],"kasten":[],"apparaten":[],"toewijzingen":[],"kastTemplates":[]}}
            """;

        var decodedOk = KeukenPersistedStateCodec.TryDecode(json, out var decoded);

        Assert.False(decodedOk);
        Assert.Empty(decoded.Wanden);
        Assert.Empty(decoded.Kasten);
    }

    [Fact]
    public void Beschadigd_versie_document_valt_niet_terug_naar_legacy()
    {
        const string json = """
            {"schemaVersion":2}
            """;

        var decodedOk = KeukenPersistedStateCodec.TryDecode(json, out var decoded);

        Assert.False(decodedOk);
        Assert.Empty(decoded.Wanden);
        Assert.Empty(decoded.Kasten);
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
