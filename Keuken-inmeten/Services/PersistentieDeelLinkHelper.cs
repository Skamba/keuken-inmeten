namespace Keuken_inmeten.Services;

public static class PersistentieDeelLinkHelper
{
    public static string MaakDeelUrl(string baseUri, string route, string token)
    {
        var basis = new Uri(baseUri);
        var routePad = string.IsNullOrWhiteSpace(route) ? "." : route.TrimStart('/');
        var routeUrl = new Uri(basis, routePad);
        var scheiding = string.IsNullOrEmpty(routeUrl.Query) ? "?" : "&";
        return $"{routeUrl}{scheiding}s={token}";
    }

    public static string BepaalRouteVoorHuidigeUrl(string relatieveUrl)
    {
        if (string.IsNullOrWhiteSpace(relatieveUrl))
            return string.Empty;

        var queryOfFragmentIndex = relatieveUrl.IndexOfAny(['?', '#']);
        return queryOfFragmentIndex >= 0
            ? relatieveUrl[..queryOfFragmentIndex]
            : relatieveUrl;
    }

    public static string? LeesTokenUitUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var queryStart = url.IndexOf('?');
        var fragmentStart = url.IndexOf('#');

        string? query = null;
        if (queryStart >= 0)
        {
            var queryEinde = fragmentStart >= 0 && fragmentStart > queryStart
                ? fragmentStart
                : url.Length;
            query = url[queryStart..queryEinde];
        }

        var fragment = fragmentStart >= 0 ? url[fragmentStart..] : null;

        return LeesParameter(query, "s")
            ?? LeesParameter(query, "share")
            ?? LeesParameter(fragment, "s")
            ?? LeesParameter(fragment, "share");
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
