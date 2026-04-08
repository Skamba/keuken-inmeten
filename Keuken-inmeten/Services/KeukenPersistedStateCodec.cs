namespace Keuken_inmeten.Services;

using System.Text.Json;
using Keuken_inmeten.Models;

public static class KeukenPersistedStateCodec
{
    private static readonly JsonSerializerOptions JsonOpties = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Encode(KeukenData data)
    {
        var document = new PersistedStateDocument
        {
            SchemaVersion = KeukenDataMigratieService.HuidigeSchemaVersie,
            Data = KeukenDataMigratieService.MaakHuidigeLokaleOpslagData(data)
        };

        return JsonSerializer.Serialize(document, JsonOpties);
    }

    public static bool TryDecode(string? json, out KeukenData data)
    {
        data = new KeukenData();

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var isVersieDocument = root.ValueKind == JsonValueKind.Object &&
                (root.TryGetProperty("schemaVersion", out _) || root.TryGetProperty("data", out _));

            return isVersieDocument
                ? TryDecodeVersieDocument(root, out data)
                : TryDecodeLegacy(json, out data);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryDecodeVersieDocument(JsonElement root, out KeukenData data)
    {
        data = new KeukenData();

        var document = root.Deserialize<PersistedStateDocument>(JsonOpties);
        if (document?.Data is null)
            return false;

        return KeukenDataMigratieService.TryMigreerLokaleOpslag(document.SchemaVersion, document.Data, out data);
    }

    private static bool TryDecodeLegacy(string json, out KeukenData data)
    {
        data = new KeukenData();

        var legacy = JsonSerializer.Deserialize<KeukenData>(json, JsonOpties);
        return KeukenDataMigratieService.TryMigreerLokaleOpslag(KeukenDataMigratieService.LegacySchemaVersie, legacy, out data);
    }

    private sealed class PersistedStateDocument
    {
        public int SchemaVersion { get; set; }
        public KeukenData? Data { get; set; }
    }
}
