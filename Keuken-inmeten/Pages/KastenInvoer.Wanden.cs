using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class KastenInvoer
{
    private void WandToevoegen()
    {
        var wandNaam = nieuweWandNaam.Trim();
        if (string.IsNullOrWhiteSpace(wandNaam))
            return;

        var wand = IndelingFormulierHelper.NieuweWand();
        wand.Naam = wandNaam;
        State.VoegWandToe(wand);
        SluitWandToevoegenModal();
    }

    private void WandToevoegenBijEnter(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            WandToevoegen();
    }

    private void OpenWandToevoegenModal()
    {
        nieuweWandNaam = "";
        toonWandToevoegenModal = true;
    }

    private void SluitWandToevoegenModal()
    {
        toonWandToevoegenModal = false;
        nieuweWandNaam = "";
    }

    private async Task WisAllesAsync()
    {
        bevestigWisAlles = false;
        SluitKastFormulier();
        SluitApparaatFormulier();
        SluitWandWerkruimte();
        await Projectbeheer.WisAllesAsync();
    }

    private void WandNaamOpslaan()
    {
        if (bewerkWandId is null || string.IsNullOrWhiteSpace(bewerkWandNaam))
            return;

        State.HernoemWand(bewerkWandId.Value, bewerkWandNaam);
        bewerkWandId = null;
    }

    private void SluitGatenOpActieveWand()
    {
        if (actieveWandId is Guid wandId)
            State.SluitAlleGatenOpWand(wandId);
    }

    private void OpenWandWerkruimte(Guid wandId)
        => StelActieveWandContextIn(wandId);

    private void SluitWandWerkruimte()
        => StelActieveWandContextIn(null);

    private void OnWandGewijzigd(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out var id))
            actieveWandId = id;
    }

    private string ActieveWandNaam()
        => actieveWandId is Guid id
            ? State.Wanden.Find(wand => wand.Id == id)?.Naam ?? "Onbekende wand"
            : "Geen wand gekozen";

    private void WijzigWandAfmeting(KeukenWand wand, string eigenschap, ChangeEventArgs e)
    {
        if (!double.TryParse(e.Value?.ToString(), out var waarde))
            return;

        var breedte = eigenschap == nameof(KeukenWand.Breedte) ? waarde : wand.Breedte;
        var hoogte = eigenschap == nameof(KeukenWand.Hoogte) ? waarde : wand.Hoogte;
        var plintHoogte = eigenschap == nameof(KeukenWand.PlintHoogte) ? waarde : wand.PlintHoogte;
        if (Math.Abs(wand.Breedte - breedte) < 0.001
            && Math.Abs(wand.Hoogte - hoogte) < 0.001
            && Math.Abs(wand.PlintHoogte - plintHoogte) < 0.001)
        {
            return;
        }

        if (!State.WerkWandAfmetingenBij(wand.Id, breedte, hoogte, plintHoogte))
            Feedback.ToonFout("Deze wandmaat past niet meer bij de huidige kasten en apparaten. Maak eerst ruimte vrij of verplaats de inhoud.");
    }

    private void BevestigVerwijderWand(Guid wandId)
    {
        var wand = State.Wanden.Find(item => item.Id == wandId);
        if (wand is null)
            return;

        var wandNaam = wand.Naam;
        State.VerwijderWand(wandId);
        bevestigVerwijderWandId = null;
        bewerkWandId = null;

        if (actieveWandId == wandId)
        {
            SluitKastFormulier();
            SluitApparaatFormulier();
            SluitWandWerkruimte();
        }

        Feedback.ToonInfo($"Wand '{wandNaam}' verwijderd.");
    }
}
