using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class PaneelConfiguratie
{
    private void ToggleKast(Guid kastId)
    {
        var wandId = VindWandId(kastId);
        if (wandId is null)
            return;

        ActiveerPaneelWerkruimte(wandId, toonEditor: true);
        VerlaatPaneelBewerkmodus();

        if (HuidigeSelectieContext.ActieveWandId is Guid actieveWandId && actieveWandId != wandId)
            WisGeselecteerdeKasten();

        if (!geselecteerdeKastIds.Add(kastId))
            geselecteerdeKastIds.Remove(kastId);

        ResetConceptPaneel();
    }

    private void DeselecteerWand(Guid wandId)
    {
        VerlaatPaneelBewerkmodus(resetFormulier: true);
        if (geopendeWandId == wandId)
            ActiveerPaneelWerkruimte(null);

        var wandKastIds = State.LeesPaneelWerkruimte(wandId)?.Kasten.Select(kast => kast.Id) ?? [];
        foreach (var id in wandKastIds)
            geselecteerdeKastIds.Remove(id);

        ResetConceptPaneel();
    }

    private void ToggleWandWerkruimte(Guid wandId)
    {
        if (geopendeWandId == wandId)
        {
            DeselecteerWand(wandId);
            return;
        }

        OpenWandWerkruimte(wandId);
    }

    private void DeselecteerAlles()
        => ResetPaneelInvoer(wisSelectie: true);

    private void OpenWandWerkruimte(Guid wandId)
    {
        if (geopendeWandId != wandId)
        {
            VerlaatPaneelBewerkmodus(resetFormulier: true);
            WisGeselecteerdeKasten();
        }

        ActiveerPaneelWerkruimte(wandId);
        ResetConceptPaneel();
    }

    private void SluitWandWerkruimte()
    {
        geopendeWandId = null;
        SluitPaneelWerklaag();
        ResetPaneelInvoer(wisSelectie: true);
    }

    private void OpenEditorDrawer()
    {
        if (geopendeWandId is not null)
            ActiveerPaneelWerkruimte(geopendeWandId, toonEditor: true);
    }

    private void SluitEditorDrawer()
    {
        SluitPaneelWerklaag();
        ResetPaneelInvoer();
    }

    private bool KanKastOpdelenIn(int aantal)
        => HuidigeSelectieContext.OpdeelBereik is { Hoogte: var hoogte }
           && hoogte >= (aantal * PaneelLayoutService.MinPaneelMaat) - 0.001;

    private void OpenKastOpdelenModal()
    {
        if (!KanKastOpdelen)
            return;

        StelOpdeelAantalIn(KanKastOpdelenIn(opdeelAantal) ? opdeelAantal : 2);
        toonKastOpdelenModal = true;
    }

    private void SluitKastOpdelenModal() => toonKastOpdelenModal = false;

    private void StelOpdeelAantalIn(int aantal)
    {
        if (!KanKastOpdelenIn(aantal) || HuidigeSelectieContext.OpdeelBereik is not { } bereik)
            return;

        opdeelAantal = aantal;
        opdeelHoogtes = MaakStandaardOpdeelHoogtes(bereik.Hoogte, aantal);
    }

    private void BevestigKastOpdelen()
    {
        var selectieContext = HuidigeSelectieContext;
        if (selectieContext.OpdeelBereik is not { } bereik || selectieContext.EnkeleGeselecteerdeKast is not { } kast)
            return;

        var analyse = PaneelConfiguratieHelper.AnalyseerOpdeelHoogtes(bereik.Hoogte, opdeelHoogtes);
        if (!analyse.KanBevestigen)
            return;

        var toewijzingen = PaneelConfiguratieHelper.BouwOpdeelSegmenten(bereik, opdeelHoogtes)
            .Select(segment => MaakPaneelToewijzing(segment, [kast]))
            .ToList();

        State.VoegToewijzingenToe(toewijzingen);
        Feedback.ToonSucces($"{toewijzingen.Count} panelen toegevoegd voor '{kast.Naam}'.");
        RondPaneelInvoerAf();
    }
}
