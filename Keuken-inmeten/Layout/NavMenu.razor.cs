using Keuken_inmeten.Services.Interop;
using Microsoft.AspNetCore.Components.Forms;

namespace Keuken_inmeten.Layout;

public partial class NavMenu
{
    private bool collapseNavMenu = true;
    private bool toonImportModal;
    private bool isDarkTheme;
    private IBrowserFile? gekozenImportBestand;
    private string? gekozenImportBestandNaam;
    private ThemeJsInterop? themeInterop;
    private int importInputVersion;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private ThemeJsInterop ThemeInterop => themeInterop ??= new(JS);

    private string ThemeLabel => isDarkTheme ? "Licht uiterlijk" : "Donker uiterlijk";

    protected override void OnInitialized()
        => State.OnStateChanged += HandleStateChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var theme = await ThemeInterop.GetThemeAsync();
            isDarkTheme = theme == "dark";
            StateHasChanged();
        }
    }

    private async Task ToggleTheme()
    {
        var newTheme = await ThemeInterop.ToggleThemeAsync();
        isDarkTheme = newTheme == "dark";
    }

    private Task DeelHuidigePagina()
        => Delen.DeelHuidigePaginaAsync();

    private Task ExporteerProject()
        => Projectbeheer.ExporteerAsync();

    private void OpenImportModal()
    {
        collapseNavMenu = true;
        toonImportModal = true;
        ResetImportSelectie(vernieuwInput: true);
    }

    private void SluitImportModal()
    {
        toonImportModal = false;
        ResetImportSelectie(vernieuwInput: true);
    }

    private void SelecteerImportBestand(InputFileChangeEventArgs args)
    {
        gekozenImportBestand = args.File;
        gekozenImportBestandNaam = args.File.Name;
    }

    private async Task BevestigImportProject()
    {
        if (gekozenImportBestand is not { } bestand)
            return;

        var importGelukt = await Projectbeheer.ImporteerAsync(bestand);
        if (importGelukt)
            toonImportModal = false;

        ResetImportSelectie(vernieuwInput: true);
    }

    private void ToggleNavMenu()
        => collapseNavMenu = !collapseNavMenu;

    private void HandleStateChanged() => _ = InvokeAsync(StateHasChanged);

    private void ResetImportSelectie(bool vernieuwInput)
    {
        gekozenImportBestand = null;
        gekozenImportBestandNaam = null;

        if (vernieuwInput)
            importInputVersion++;
    }

    public async ValueTask DisposeAsync()
    {
        State.OnStateChanged -= HandleStateChanged;

        if (themeInterop is not null)
            await themeInterop.DisposeAsync();
    }
}
