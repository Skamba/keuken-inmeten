namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;
using Microsoft.JSInterop;

public partial class PersistentieService
{
    private void OpStateGewijzigd()
    {
        if (_bewaarGepland)
            return;

        _bewaarGepland = true;
        ZetOpslagStatus(OpslagStatusType.Bezig, "Automatisch opslaan...");
        _ = BewaarDebounced();
    }

    private async Task BewaarDebounced()
    {
        await Task.Delay(300);
        _bewaarGepland = false;
        await SlaanAsync();
    }

    public async Task LadenAsync()
    {
        HeeftGedeeldeLinkGeladen = false;
        HeeftOngeldigeDeelLink = false;

        var (heeftGedeeldeLink, gedeeldeData) = await LeesGedeeldeDataUitUrlAsync();
        if (heeftGedeeldeLink)
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
            var json = KeukenPersistedStateCodec.Encode(_state.Exporteren());
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
}
