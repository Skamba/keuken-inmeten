using System.Linq;
using Keuken_inmeten.Services;
using Keuken_inmeten.Services.Interop;
using Microsoft.AspNetCore.Components.Forms;

namespace Keuken_inmeten.Pages;

public partial class Project
{
    private bool isDarkTheme;
    private bool toonWisProjectBevestiging;
    private IBrowserFile? gekozenImportBestand;
    private string? gekozenImportBestandNaam;
    private ThemeJsInterop? themeInterop;
    private int importInputVersion;

    private ThemeJsInterop ThemeInterop => themeInterop ??= new(JS);

    private string ThemeLabel => isDarkTheme ? "Licht uiterlijk" : "Donker uiterlijk";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var theme = await ThemeInterop.GetThemeAsync();
        isDarkTheme = theme == "dark";
        StateHasChanged();
    }

    private ProjectPaginaModel MaakPaginaModel()
    {
        var resultaten = State.BerekenResultaten();
        var bestellijstItems = BestellijstService.BerekenItems(State, resultaten);

        return ProjectReadModelHelper.BouwPaginaModel(
            aantalWanden: State.Wanden.Count,
            aantalKasten: State.Kasten.Count,
            aantalPanelen: State.Toewijzingen.Count,
            aantalBestellijstItems: bestellijstItems.Count,
            totaalBoorgaten: bestellijstItems.Sum(item => item.Aantal * item.Boorgaten.Count),
            heeftProjectInhoud: State.HeeftProjectInhoud(),
            paneelRandSpeling: State.PaneelRandSpeling,
            isDarkTheme: isDarkTheme);
    }

    private double ProjectRandSpelingInput
    {
        get => State.PaneelRandSpeling;
        set => State.StelPaneelRandSpelingIn(value);
    }

    private Task DeelProject()
        => Delen.DeelKeukenAsync("project");

    private Task ExporteerProject()
        => Projectbeheer.ExporteerAsync();

    private async Task ToggleTheme()
    {
        var newTheme = await ThemeInterop.ToggleThemeAsync();
        isDarkTheme = newTheme == "dark";
    }

    private void ToggleWisProjectBevestiging()
        => toonWisProjectBevestiging = !toonWisProjectBevestiging;

    private void SluitWisProjectBevestiging()
        => toonWisProjectBevestiging = false;

    private void SelecteerImportBestand(InputFileChangeEventArgs args)
    {
        gekozenImportBestand = args.File;
        gekozenImportBestandNaam = args.File.Name;
        toonWisProjectBevestiging = false;
    }

    private async Task BevestigImportProject()
    {
        if (gekozenImportBestand is not { } bestand)
            return;

        var importGelukt = await Projectbeheer.ImporteerAsync(bestand);
        if (importGelukt)
            toonWisProjectBevestiging = false;

        ResetImportSelectie(vernieuwInput: true);
    }

    private async Task BevestigWisProject()
    {
        toonWisProjectBevestiging = false;
        await Projectbeheer.WisAllesAsync();
    }

    private void ResetImportSelectie(bool vernieuwInput)
    {
        gekozenImportBestand = null;
        gekozenImportBestandNaam = null;

        if (vernieuwInput)
            importInputVersion++;
    }

    public async ValueTask DisposeAsync()
    {
        if (themeInterop is not null)
            await themeInterop.DisposeAsync();
    }
}
