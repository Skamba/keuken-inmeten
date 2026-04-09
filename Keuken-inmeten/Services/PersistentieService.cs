namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public class PersistentieService : IDisposable
{
    private const string StorageKey = "keuken-inmeten-data";

    private readonly IJSRuntime _js;
    private readonly KeukenStateService _state;
    private readonly NavigationManager _navigation;
    private bool _bewaarGepland;
    public bool HeeftGedeeldeLinkGeladen { get; private set; }
    public bool HeeftOngeldigeDeelLink { get; private set; }
    public event Action? OnOpslagStatusChanged;
    public bool HeeftOpslagStatus => !string.IsNullOrWhiteSpace(OpslagStatusTekst);
    public string? OpslagStatusTekst { get; private set; }
    public OpslagStatusType OpslagStatus { get; private set; } = OpslagStatusType.Opgeslagen;

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
        ZetOpslagStatus(OpslagStatusType.Bezig, "Automatisch opslaan...");
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

        string? json;
        try
        {
            json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }
        catch (JSException)
        {
            ZetOpslagStatus(OpslagStatusType.Fout, "Opgeslagen keuken laden lukt niet in deze browser.");
            return;
        }

        if (string.IsNullOrEmpty(json))
            return;

        if (!KeukenPersistedStateCodec.TryDecode(json, out var data))
        {
            ZetOpslagStatus(
                OpslagStatusType.Fout,
                "De opgeslagen keuken is van een onbekende versie of beschadigd en kon niet worden geladen.");
            return;
        }

        _state.Laden(data);
    }

    public string MaakDeelUrl(string route = "verificatie")
    {
        var token = KeukenShareCodec.Encode(_state.Exporteren());
        var basis = new Uri(_navigation.BaseUri);
        var routePad = string.IsNullOrWhiteSpace(route) ? "." : route.TrimStart('/');
        var routeUrl = new Uri(basis, routePad);
        var scheiding = string.IsNullOrEmpty(routeUrl.Query) ? "?" : "&";
        return $"{routeUrl}{scheiding}s={token}";
    }

    public string MaakDeelUrlVoorHuidigeRoute()
    {
        var relatieveUrl = _navigation.ToBaseRelativePath(_navigation.Uri);
        var queryOfFragmentIndex = relatieveUrl.IndexOfAny(['?', '#']);
        var route = queryOfFragmentIndex >= 0
            ? relatieveUrl[..queryOfFragmentIndex]
            : relatieveUrl;

        return MaakDeelUrl(route);
    }

    public string ExporteerProjectJson()
        => KeukenPersistedStateCodec.Encode(_state.Exporteren());

    public bool TryDecodeProjectJson(string? json, out KeukenData data)
        => KeukenPersistedStateCodec.TryDecode(json, out data);

    public async Task ImporteerProjectAsync(KeukenData data)
    {
        _state.Importeer(data);
        await SlaanAsync();
    }

    public async Task ResetProjectAsync()
    {
        _state.VerwijderAlles();
        await SlaanAsync();
    }

    public async Task SlaanAsync()
    {
        try
        {
            var data = _state.Exporteren();
            var json = KeukenPersistedStateCodec.Encode(data);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
            ZetOpslagStatus(OpslagStatusType.Opgeslagen, $"Automatisch opgeslagen om {DateTime.Now:HH:mm}");
        }
        catch (JSException)
        {
            ZetOpslagStatus(
                OpslagStatusType.Fout,
                "Automatisch opslaan lukt niet. Houd dit tabblad open of deel een link.");
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
        var token = LeesParameter(uri.Query, "s")
            ?? LeesParameter(uri.Query, "share")
            ?? LeesParameter(uri.Fragment, "s")
            ?? LeesParameter(uri.Fragment, "share");
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

    private void ZetOpslagStatus(OpslagStatusType status, string tekst)
    {
        OpslagStatus = status;
        OpslagStatusTekst = tekst;
        OnOpslagStatusChanged?.Invoke();
    }
}

public enum OpslagStatusType
{
    Bezig,
    Opgeslagen,
    Fout
}
