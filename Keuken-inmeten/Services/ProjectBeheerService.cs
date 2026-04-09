namespace Keuken_inmeten.Services;

using System.Text;
using Keuken_inmeten.Services.Interop;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

public sealed class ProjectBeheerService(IJSRuntime js, PersistentieService persistentie, ActieFeedbackService feedback) : IAsyncDisposable
{
    private const long MaxImportBestandGrootte = 5 * 1024 * 1024;

    private BrowserWindowJsInterop? _browserWindowInterop;

    private BrowserWindowJsInterop BrowserWindowInterop => _browserWindowInterop ??= new(js);

    public async Task ExporteerAsync()
    {
        try
        {
            await BrowserWindowInterop.DownloadTextFileAsync(
                MaakBestandsnaam(),
                persistentie.ExporteerProjectJson(),
                "application/json;charset=utf-8");
            feedback.ToonSucces("Projectbestand gedownload. Bewaar het JSON-bestand lokaal of deel het handmatig.");
        }
        catch (JSException)
        {
            feedback.ToonFout("Projectbestand downloaden lukte niet. Controleer of downloads in deze browser zijn toegestaan.");
        }
    }

    public async Task<bool> ImporteerAsync(IBrowserFile bestand)
    {
        string json;

        try
        {
            await using var stream = bestand.OpenReadStream(MaxImportBestandGrootte);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            json = await reader.ReadToEndAsync();
        }
        catch (IOException)
        {
            feedback.ToonFout("Importeren lukte niet. Kies een leesbaar JSON-bestand tot 5 MB.");
            return false;
        }

        if (!persistentie.TryDecodeProjectJson(json, out var data))
        {
            feedback.ToonFout("Het gekozen bestand is geen geldig keukenproject. Exporteer opnieuw of kies een ongewijzigd JSON-bestand.");
            return false;
        }

        await persistentie.ImporteerProjectAsync(data);
        feedback.ToonSucces($"Project '{bestand.Name}' is geladen. U kunt direct verder met meten en controleren.");
        return true;
    }

    public async Task WisAllesAsync()
    {
        await persistentie.ResetProjectAsync();
        feedback.ToonSucces("Het keukenproject is gewist. U kunt direct opnieuw beginnen.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_browserWindowInterop is not null)
            await _browserWindowInterop.DisposeAsync();
    }

    private static string MaakBestandsnaam()
        => $"keuken-inmeten-{DateTime.Now:yyyyMMdd-HHmm}.json";
}
