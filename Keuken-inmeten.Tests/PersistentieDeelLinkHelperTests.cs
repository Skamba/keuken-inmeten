using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PersistentieDeelLinkHelperTests
{
    [Theory]
    [InlineData("verificatie?s=v4.token", "verificatie")]
    [InlineData("verificatie#share=legacy", "verificatie")]
    [InlineData("verificatie?tab=overzicht#share=v4.token", "verificatie")]
    [InlineData("", "")]
    public void BepaalRouteVoorHuidigeUrl_stript_query_en_fragment(string relatieveUrl, string verwacht)
    {
        var route = PersistentieDeelLinkHelper.BepaalRouteVoorHuidigeUrl(relatieveUrl);

        Assert.Equal(verwacht, route);
    }

    [Fact]
    public void MaakDeelUrl_bewaart_bestaande_query_en_voegt_share_token_toe()
    {
        var url = PersistentieDeelLinkHelper.MaakDeelUrl(
            "https://example.com/app/",
            "verificatie?tab=overzicht",
            "v4.token");

        Assert.Equal("https://example.com/app/verificatie?tab=overzicht&s=v4.token", url);
    }

    [Theory]
    [InlineData("https://example.com/app/verificatie?s=v4.token", "v4.token")]
    [InlineData("https://example.com/app/verificatie?share=legacy%2Etoken", "legacy.token")]
    [InlineData("https://example.com/app/verificatie#s=fragment-token", "fragment-token")]
    [InlineData("https://example.com/app/verificatie?s=query-token#share=fragment-token", "query-token")]
    [InlineData("https://example.com/app/verificatie", null)]
    public void LeesTokenUitUrl_leest_query_en_fragment_varianten(string url, string? verwacht)
    {
        var token = PersistentieDeelLinkHelper.LeesTokenUitUrl(url);

        Assert.Equal(verwacht, token);
    }
}
