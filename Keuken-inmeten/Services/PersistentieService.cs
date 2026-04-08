namespace Keuken_inmeten.Services;

using System.Text.Json;
using Keuken_inmeten.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public class PersistentieService : IDisposable
{
    private const string StorageKey = "keuken-inmeten-data";
    private static readonly JsonSerializerOptions JsonOpties = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IJSRuntime _js;
    private readonly KeukenStateService _state;
    private readonly NavigationManager _navigation;
    private bool _bewaarGepland;
    public bool HeeftGedeeldeLinkGeladen { get; private set; }
    public bool HeeftOngeldigeDeelLink { get; private set; }

    public PersistentieService(IJSRuntime js, KeukenStateService state, NavigationManager navigation)
    {
        _js = js;
        _state = state;
        _navigation = navigation;
        _state.OnStateChanged += OpStateGewijzigd;
    }

    private void OpStateGewijzigd()
    {
        if (_bewaarGepland) return;
        _bewaarGepland = true;
        _ = BewaarDebounced();
    }

    private async Task BewaarDebounced()
    {
        // Small delay so multiple rapid mutations are batched into one write
        await Task.Delay(300);
        _bewaarGepland = false;
        await SlaanAsync();
    }

    public async Task LadenAsync()
    {
        HeeftGedeeldeLinkGeladen = false;
        HeeftOngeldigeDeelLink = false;

        if (TryLeesGedeeldeDataUitUrl(out var gedeeldeData))
        {
            if (gedeeldeData is not null)
            {
                HeeftGedeeldeLinkGeladen = true;
                _state.Laden(gedeeldeData);
                await SlaanAsync();
            }
            else
            {
                HeeftOngeldigeDeelLink = true;
            }

            return;
        }

        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json)) return;

            var data = JsonSerializer.Deserialize<KeukenData>(json, JsonOpties);
            if (data is not null)
                _state.Laden(data);
        }
        catch
        {
            // Corrupt or incompatible data — start fresh
        }
    }

    public string MaakDeelUrl(string route = "verificatie")
    {
        var token = KeukenShareCodec.Encode(_state.Exporteren());
        var basis = new Uri(_navigation.BaseUri);
        var routePad = string.IsNullOrWhiteSpace(route) ? "." : route.TrimStart('/');
        var routeUrl = new Uri(basis, routePad);
        var scheiding = string.IsNullOrEmpty(routeUrl.Query) ? "?" : "&";
        return $"{routeUrl}{scheiding}share={token}";
    }

    public async Task SlaanAsync()
    {
        try
        {
            var data = _state.Exporteren();
            var json = JsonSerializer.Serialize(data, JsonOpties);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch
        {
            // Storage quota exceeded or unavailable — silently ignore
        }
    }

    public async Task VerwijderenAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    private bool TryLeesGedeeldeDataUitUrl(out KeukenData? data)
    {
        data = null;

        var uri = new Uri(_navigation.Uri);
        var token = LeesParameter(uri.Query, "share") ?? LeesParameter(uri.Fragment, "share");
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (!KeukenShareCodec.TryDecode(token, out var gedeeldeData))
            return true;

        data = gedeeldeData;
        return true;
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

    public void Dispose()
    {
        _state.OnStateChanged -= OpStateGewijzigd;
    }
}
