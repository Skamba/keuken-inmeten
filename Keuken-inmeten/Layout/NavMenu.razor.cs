using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Keuken_inmeten.Services.Interop;
using Microsoft.AspNetCore.Components.Forms;

namespace Keuken_inmeten.Layout;

public partial class NavMenu
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool collapseNavMenu = true;
    private bool toonImportModal;
    private bool toonWisProjectBevestiging;
    private bool isDarkTheme;
    private IBrowserFile? gekozenImportBestand;
    private string? gekozenImportBestandNaam;
    private ThemeJsInterop? themeInterop;
    private int importInputVersion;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private ThemeJsInterop ThemeInterop => themeInterop ??= new(JS);

    private string ThemeLabel => isDarkTheme ? "Licht uiterlijk" : "Donker uiterlijk";

    protected override void OnInitialized()
    {
        State.OnStateChanged += HandleStateChanged;
        Navigation.LocationChanged += HandleLocationChanged;
    }

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
        toonWisProjectBevestiging = false;
        toonImportModal = true;
        ResetImportSelectie(vernieuwInput: true);
    }

    private void SluitImportModal()
    {
        toonImportModal = false;
        ResetImportSelectie(vernieuwInput: true);
    }

    private void ToggleWisProjectBevestiging()
    {
        collapseNavMenu = true;
        toonImportModal = false;
        toonWisProjectBevestiging = !toonWisProjectBevestiging;
    }

    private void SluitWisProjectBevestiging()
        => toonWisProjectBevestiging = false;

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

    private async Task BevestigWisProject()
    {
        toonWisProjectBevestiging = false;
        await Projectbeheer.WisAllesAsync();
    }

    private void ToggleNavMenu()
        => collapseNavMenu = !collapseNavMenu;

    private void HandleStateChanged()
    {
        if (!State.HeeftProjectInhoud())
            toonWisProjectBevestiging = false;

        _ = InvokeAsync(StateHasChanged);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
        => _ = InvokeAsync(StateHasChanged);

    private static string? BepaalWandNavTestIdPrefix(string stapId)
        => stapId switch
        {
            "kasten" => "indeling",
            "panelen" => "panelen",
            "verificatie" => "verificatie",
            _ => null
        };

    private static string MaakWandRoute(string stapId, Guid wandId)
        => stapId switch
        {
            "kasten" => $"kasten?wand={wandId:D}",
            "panelen" => $"panelen?wand={wandId:D}",
            "verificatie" => $"verificatie?wand={wandId:D}",
            _ => throw new ArgumentOutOfRangeException(nameof(stapId), stapId, "Onbekende stap voor wandnavigatie.")
        };

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
        Navigation.LocationChanged -= HandleLocationChanged;

        if (themeInterop is not null)
            await themeInterop.DisposeAsync();
    }
}
