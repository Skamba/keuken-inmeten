namespace Keuken_inmeten.Services;

using Keuken_inmeten.Services.Interop;
using Microsoft.JSInterop;

public sealed class DeelLinkService(IJSRuntime js, PersistentieService persistentie, ActieFeedbackService feedback) : IAsyncDisposable
{
    private BrowserWindowJsInterop? _browserWindowInterop;

    private BrowserWindowJsInterop BrowserWindowInterop => _browserWindowInterop ??= new(js);

    public Task DeelHuidigePaginaAsync()
        => DeelUrlAsync(persistentie.MaakDeelUrlVoorHuidigeRoute());

    public Task DeelKeukenAsync(string route)
        => DeelUrlAsync(persistentie.MaakDeelUrl(route));

    private async Task DeelUrlAsync(string deelUrl)
    {
        try
        {
            var resultaat = await BrowserWindowInterop.ShareUrlAsync(deelUrl, "Keuken-inmeten", "Bekijk deze keukenconfiguratie");

            switch (resultaat)
            {
                case "shared":
                    feedback.ToonSucces("De share-sheet is geopend. Kies nu de app of het contact waarmee u wilt delen.");
                    break;
                case "copied":
                    feedback.ToonSucces("De deellink is gekopieerd. Plak hem nu in mail, chat of notities.");
                    break;
                case "cancelled":
                    feedback.ToonInfo("Delen geannuleerd. U kunt opnieuw delen of de URL uit de adresbalk kopieren.");
                    break;
                default:
                    feedback.ToonFout("Delen lukt hier niet. Kopieer de URL uit de adresbalk en deel die handmatig.");
                    break;
            }
        }
        catch (JSException)
        {
            feedback.ToonFout("Deellink maken of kopieren lukte niet. Controleer of de browser delen of klembordtoegang toestaat.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browserWindowInterop is not null)
            await _browserWindowInterop.DisposeAsync();
    }
}
