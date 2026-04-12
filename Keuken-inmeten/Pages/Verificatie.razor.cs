using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Keuken_inmeten.Services.Interop;

namespace Keuken_inmeten.Pages;

public partial class Verificatie
{
    private enum VerificatieFase { Overzicht, PaneelVerificatie, Afronding }

    private VerificatieFase _fase = VerificatieFase.Overzicht;
    private int _paneelIndex;
    private BrowserWindowJsInterop? _browserWindowInterop;

    private BrowserWindowJsInterop BrowserWindowInterop => _browserWindowInterop ??= new(JS);

    protected override void OnInitialized()
        => State.OnStateChanged += HandleStateChanged;

    private void HandleStateChanged()
        => _ = InvokeAsync(StateHasChanged);

    private PaneelVerificatieStatus GetChecks(PaneelResultaat resultaat)
        => State.LeesVerificatieStatus(resultaat.ToewijzingId);

    private void ToggleMatenCheck(PaneelResultaat resultaat)
    {
        var checks = GetChecks(resultaat);
        State.WerkVerificatieStatusBij(resultaat.ToewijzingId, !checks.MatenOk, checks.ScharnierPositiesOk);
    }

    private void ToggleScharnierCheck(PaneelResultaat resultaat)
    {
        var checks = GetChecks(resultaat);
        State.WerkVerificatieStatusBij(resultaat.ToewijzingId, checks.MatenOk, !checks.ScharnierPositiesOk);
    }

    private static bool HeeftScharnierCheck(PaneelResultaat r)
        => r.Type == PaneelType.Deur && r.Boorgaten.Count > 0;

    private bool AlleChecksVoorPaneel(int index, PaneelResultaat r)
    {
        var checks = GetChecks(r);

        if (!checks.MatenOk)
            return false;

        return !HeeftScharnierCheck(r) || checks.ScharnierPositiesOk;
    }

    private int AantalAfgevinktVoorPaneel(int index, PaneelResultaat r)
    {
        var checks = GetChecks(r);

        var count = checks.MatenOk ? 1 : 0;
        if (HeeftScharnierCheck(r) && checks.ScharnierPositiesOk)
            count++;

        return count;
    }

    private static int TotaalChecksVoorPaneel(PaneelResultaat r)
        => HeeftScharnierCheck(r) ? 2 : 1;

    private int OpenChecksVoorPaneel(int index, PaneelResultaat r)
        => TotaalChecksVoorPaneel(r) - AantalAfgevinktVoorPaneel(index, r);

    private string OpenChecksLabel(int index, PaneelResultaat r)
    {
        var open = OpenChecksVoorPaneel(index, r);
        return open == 0 ? "Alles klaar" : $"{open} open";
    }

    private string VolgendeControleTitel(int index, PaneelResultaat r)
    {
        var checks = GetChecks(r);

        if (!checks.MatenOk)
            return "Meet de maat in de opening na";

        if (HeeftScharnierCheck(r) && !checks.ScharnierPositiesOk)
            return "Controleer de systeemgaten";

        return "Alle controles voor dit paneel zijn klaar";
    }

    private string HuidigeControleHint(int index, PaneelResultaat r)
        => VolgendeControleTitel(index, r) switch
        {
            "Meet de maat in de opening na" => "Vergelijk de echte opening met de maat hieronder voordat u verdergaat.",
            "Controleer de systeemgaten" => "Bevestig nu of de scharnierplaatposities in de gaatjesrij kloppen.",
            _ => "Alle checkliststappen voor dit paneel zijn afgevinkt."
        };

    private string PaneelTabStatusTekst(int index, PaneelResultaat r)
        => AlleChecksVoorPaneel(index, r)
            ? "Klaar"
            : $"{AantalAfgevinktVoorPaneel(index, r)}/{TotaalChecksVoorPaneel(r)} controles";

    private void GaNaarPaneel(int index)
    {
        _paneelIndex = index;
        _fase = VerificatieFase.PaneelVerificatie;
    }

    private void VorigePaneel()
    {
        if (_paneelIndex > 0)
            _paneelIndex--;
        else
            _fase = VerificatieFase.Overzicht;
    }

    private void VolgendPaneel()
    {
        var resultaten = State.BerekenResultaten();
        if (_paneelIndex < resultaten.Count - 1)
            _paneelIndex++;
        else
            _fase = VerificatieFase.Afronding;
    }

    private void TerugNaarOverzicht() => _fase = VerificatieFase.Overzicht;

    private static string TypeNaam(PaneelType type) => VisualisatieHelper.PaneelTypeLabel(type);

    private static string TypeBadgeClass(PaneelType type) => type switch
    {
        PaneelType.Deur => "bg-primary",
        PaneelType.LadeFront => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    private static string FormatMm(double waarde)
        => $"{waarde:0.#} mm";

    private static string? PaneelbeeldSpelingToelichting(PaneelResultaat resultaat)
    {
        var maatInfo = resultaat.MaatInfo;
        if (maatInfo is null)
            return null;

        var boven = maatInfo.InkortingBoven;
        var onder = maatInfo.InkortingOnder;

        if (boven > 0.001 && onder > 0.001)
            return $"Paneel is {FormatMm(boven)} ingekort aan de bovenzijde en {FormatMm(onder)} aan de onderzijde.";

        if (boven > 0.001)
            return $"Paneel is {FormatMm(boven)} ingekort aan de bovenzijde.";

        if (onder > 0.001)
            return $"Paneel is {FormatMm(onder)} ingekort aan de onderzijde.";

        return null;
    }

    private static string FormatUitlijnAfwijkingen(IReadOnlyList<UitlijnAfwijking> afwijkingen)
        => string.Join(", ", afwijkingen.Select(afwijking => $"{afwijking.Label} {FormatMm(afwijking.AfwijkingMm)}"));

    private async Task PrintResultaat()
        => await BrowserWindowInterop.PrintCurrentPageAsync();

    private Task DeelResultaat()
        => Delen.DeelKeukenAsync("verificatie");

    public async ValueTask DisposeAsync()
    {
        State.OnStateChanged -= HandleStateChanged;
        if (_browserWindowInterop is not null)
            await _browserWindowInterop.DisposeAsync();
    }
}
