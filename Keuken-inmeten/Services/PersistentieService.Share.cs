namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;
using Microsoft.JSInterop;

public partial class PersistentieService
{
    public async Task<string> MaakDeelUrlAsync(string route = "verificatie")
    {
        var compactJson = KeukenShareCodec.EncodeCompactJson(_state.Exporteren());
        var compressedPayload = await ShareCompressionInterop.CompressSharePayloadAsync(compactJson);
        var token = KeukenShareCodec.MaakV4Token(compressedPayload);
        var basis = new Uri(_navigation.BaseUri);
        var routePad = string.IsNullOrWhiteSpace(route) ? "." : route.TrimStart('/');
        var routeUrl = new Uri(basis, routePad);
        var scheiding = string.IsNullOrEmpty(routeUrl.Query) ? "?" : "&";
        return $"{routeUrl}{scheiding}s={token}";
    }

    public async Task<string> MaakDeelUrlVoorHuidigeRouteAsync()
    {
        var relatieveUrl = _navigation.ToBaseRelativePath(_navigation.Uri);
        var queryOfFragmentIndex = relatieveUrl.IndexOfAny(['?', '#']);
        var route = queryOfFragmentIndex >= 0
            ? relatieveUrl[..queryOfFragmentIndex]
            : relatieveUrl;

        return await MaakDeelUrlAsync(route);
    }

    public string ExporteerProjectJson()
        => KeukenPersistedStateCodec.Encode(_state.Exporteren());

    public bool TryDecodeProjectJson(string? json, out KeukenData data)
        => KeukenPersistedStateCodec.TryDecode(json, out data);

    private async Task<(bool heeftLink, KeukenData? data)> LeesGedeeldeDataUitUrlAsync()
    {
        var uri = new Uri(_navigation.Uri);
        var token = LeesParameter(uri.Query, "s")
            ?? LeesParameter(uri.Query, "share")
            ?? LeesParameter(uri.Fragment, "s")
            ?? LeesParameter(uri.Fragment, "share");
        if (string.IsNullOrWhiteSpace(token))
            return (false, null);

        if (KeukenShareCodec.IsV4Token(token))
        {
            try
            {
                var compactJson = await ShareCompressionInterop.DecompressSharePayloadAsync(KeukenShareCodec.LeesV4Payload(token));
                return (true, KeukenShareCodec.TryDecodeCompactJson(compactJson, out var v4Data) ? v4Data : null);
            }
            catch (JSException)
            {
                return (true, null);
            }
        }

        return (true, KeukenShareCodec.TryDecode(token, out var gedeeldeData) ? gedeeldeData : null);
    }

    private static string? LeesParameter(string? bron, string naam)
    {
        if (string.IsNullOrWhiteSpace(bron))
            return null;

        var delen = bron.TrimStart('?', '#')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var deel in delen)
        {
            var keyValue = deel.Split('=', 2);
            if (!string.Equals(keyValue[0], naam, StringComparison.OrdinalIgnoreCase))
                continue;

            return keyValue.Length > 1
                ? Uri.UnescapeDataString(keyValue[1])
                : string.Empty;
        }

        return null;
    }
}
