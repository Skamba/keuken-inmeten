namespace Keuken_inmeten.Services;

using System.Text.Json;
using Keuken_inmeten.Models;

public static class KeukenShareCodec
{
    private const string VersiePrefix = "v1.";

    private static readonly JsonSerializerOptions JsonOpties = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Encode(KeukenData data)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(MaakDeelbareData(data), JsonOpties);
        return VersiePrefix + NaarBase64Url(json);
    }

    public static bool TryDecode(string? token, out KeukenData data)
    {
        data = new KeukenData();

        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(VersiePrefix, StringComparison.Ordinal))
            return false;

        try
        {
            var json = VanBase64Url(token[VersiePrefix.Length..]);
            var decoded = JsonSerializer.Deserialize<KeukenData>(json, JsonOpties);
            if (decoded is null)
                return false;

            data = Normaliseer(decoded);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static KeukenData MaakDeelbareData(KeukenData data) => new()
    {
        Wanden = [.. data.Wanden],
        Kasten = [.. data.Kasten],
        Apparaten = [.. data.Apparaten],
        Toewijzingen = [.. data.Toewijzingen],
        KastTemplates = [],
        LaatstGebruiktePotHartVanRand = data.LaatstGebruiktePotHartVanRand
    };

    private static KeukenData Normaliseer(KeukenData data) => new()
    {
        Wanden = data.Wanden ?? [],
        Kasten = data.Kasten ?? [],
        Apparaten = data.Apparaten ?? [],
        Toewijzingen = data.Toewijzingen ?? [],
        KastTemplates = data.KastTemplates ?? [],
        LaatstGebruiktePotHartVanRand = data.LaatstGebruiktePotHartVanRand
    };

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
