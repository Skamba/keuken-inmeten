namespace Keuken_inmeten.Services;

using System.Text.Json;
using Keuken_inmeten.Models;
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
    private bool _bewaarGepland;

    public PersistentieService(IJSRuntime js, KeukenStateService state)
    {
        _js = js;
        _state = state;
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

    public void Dispose()
    {
        _state.OnStateChanged -= OpStateGewijzigd;
    }
}
