namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;
using Microsoft.JSInterop;

public partial class PersistentieService
{
    public async Task<string> MaakDeelUrlAsync(string route = "verificatie")
    {
        var shareJson = KeukenShareCodec.EncodeV4Json(_state.Exporteren());
        var compressedPayload = await ShareCompressionInterop.CompressSharePayloadAsync(shareJson);
        var token = KeukenShareCodec.MaakV4Token(compressedPayload);
        return PersistentieDeelLinkHelper.MaakDeelUrl(_navigation.BaseUri, route, token);
    }

    public async Task<string> MaakDeelUrlVoorHuidigeRouteAsync()
    {
        var route = PersistentieDeelLinkHelper.BepaalRouteVoorHuidigeUrl(_navigation.ToBaseRelativePath(_navigation.Uri));
        return await MaakDeelUrlAsync(route);
    }

    public string ExporteerProjectJson()
        => KeukenPersistedStateCodec.Encode(_state.Exporteren());

    public bool TryDecodeProjectJson(string? json, out KeukenData data)
        => KeukenPersistedStateCodec.TryDecode(json, out data);

    private async Task<(bool heeftLink, KeukenData? data)> LeesGedeeldeDataUitUrlAsync()
    {
        var token = PersistentieDeelLinkHelper.LeesTokenUitUrl(_navigation.Uri);
        if (string.IsNullOrWhiteSpace(token))
            return (false, null);

        if (KeukenShareCodec.IsV4Token(token))
        {
            try
            {
                var shareJson = await ShareCompressionInterop.DecompressSharePayloadAsync(KeukenShareCodec.LeesV4Payload(token));
                return (true, KeukenShareCodec.TryDecodeV4Json(shareJson, out var v4Data) ? v4Data : null);
            }
            catch (JSException)
            {
                return (true, null);
            }
        }

        return (true, KeukenShareCodec.TryDecode(token, out var gedeeldeData) ? gedeeldeData : null);
    }
}
