namespace Keuken_inmeten.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using Keuken_inmeten.Models;

public static partial class KeukenShareCodec
{
    private const string VersiePrefixV1 = "v1.";
    private const string VersiePrefixV2 = "v2.";
    private const string VersiePrefixV3 = "v3.";
    private const string VersiePrefixV4 = "v4.";
    private static readonly KeukenWand DefaultWand = KeukenDomeinDefaults.NieuweWand();
    private static readonly Kast DefaultKast = KeukenDomeinDefaults.NieuweKast();

    private static readonly JsonSerializerOptions LegacyJsonOpties = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions CompactJsonOpties = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string Encode(KeukenData data)
    {
        var compact = MaakCompacteDataVoorShare(data);
        var json = JsonSerializer.SerializeToUtf8Bytes(compact, CompactJsonOpties);
        return VersiePrefixV3 + NaarBase64Url(json);
    }

    public static string EncodeCompactJson(KeukenData data)
        => JsonSerializer.Serialize(MaakCompacteDataVoorShare(data), CompactJsonOpties);

    public static bool TryDecodeCompactJson(string? json, out KeukenData data)
    {
        data = new KeukenData();

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            var decoded = JsonSerializer.Deserialize<CompactShareData>(json, CompactJsonOpties);
            return TryDecodeCompactData(decoded, out data);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool IsV4Token(string? token)
        => token?.StartsWith(VersiePrefixV4, StringComparison.Ordinal) == true;

    public static string MaakV4Token(string compressedPayload)
        => VersiePrefixV4 + compressedPayload;

    public static string LeesV4Payload(string token)
        => token[VersiePrefixV4.Length..];

    public static bool TryDecode(string? token, out KeukenData data)
    {
        data = new KeukenData();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (token.StartsWith(VersiePrefixV3, StringComparison.Ordinal))
            return TryDecodeV3(token, out data);

        if (token.StartsWith(VersiePrefixV2, StringComparison.Ordinal))
            return TryDecodeV2(token, out data);

        if (token.StartsWith(VersiePrefixV1, StringComparison.Ordinal))
            return TryDecodeV1(token, out data);

        return false;
    }

    private static bool TryDecodeV3(string token, out KeukenData data)
    {
        data = new KeukenData();

        try
        {
            var json = VanBase64Url(token[VersiePrefixV3.Length..]);
            var decoded = JsonSerializer.Deserialize<CompactShareData>(json, CompactJsonOpties);
            return TryDecodeCompactData(decoded, out data);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryDecodeV2(string token, out KeukenData data)
    {
        data = new KeukenData();

        try
        {
            var json = VanBase64Url(token[VersiePrefixV2.Length..]);
            var decoded = JsonSerializer.Deserialize<CompactShareData>(json, CompactJsonOpties);
            return TryDecodeCompactData(decoded, out data);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryDecodeV1(string token, out KeukenData data)
    {
        data = new KeukenData();

        try
        {
            var json = VanBase64Url(token[VersiePrefixV1.Length..]);
            var decoded = JsonSerializer.Deserialize<KeukenData>(json, LegacyJsonOpties);
            return KeukenDataMigratieService.TryMigreerDeelData(KeukenDataMigratieService.LegacySchemaVersie, decoded, out data);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static CompactShareData MaakCompacteDataVoorShare(KeukenData data)
        => MaakCompacteData(KeukenDataMigratieService.MaakHuidigeDeelData(data));

    private static bool TryDecodeCompactData(CompactShareData? decoded, out KeukenData data)
    {
        data = new KeukenData();

        if (decoded is null)
            return false;

        var schemaVersie = decoded.SchemaVersion ?? KeukenDataMigratieService.PerPaneelSpelingSchemaVersie;
        var decodedData = BouwKeukenData(decoded, schemaVersie);
        return KeukenDataMigratieService.TryMigreerDeelData(schemaVersie, decodedData, out data);
    }

    private static double Round1(double waarde) => Math.Round(waarde, 1);

    private static bool IsBijnaGelijk(double links, double rechts) => Math.Abs(links - rechts) < 0.001;

    private static string NaarBase64Url(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] VanBase64Url(string base64Url)
    {
        var padded = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        var remainder = padded.Length % 4;
        if (remainder > 0)
            padded = padded.PadRight(padded.Length + (4 - remainder), '=');

        return Convert.FromBase64String(padded);
    }
}
