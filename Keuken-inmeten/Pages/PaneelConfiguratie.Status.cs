using Keuken_inmeten.Models;

namespace Keuken_inmeten.Pages;

public partial class PaneelConfiguratie
{
    private void WisGeselecteerdeKasten()
        => geselecteerdeKastIds.Clear();

    private void StelGeselecteerdeKastenIn(IEnumerable<Guid> kastIds)
    {
        WisGeselecteerdeKasten();
        foreach (var kastId in kastIds)
            geselecteerdeKastIds.Add(kastId);
    }

    private void VerlaatPaneelBewerkmodus(bool resetFormulier = false)
    {
        bewerkToewijzingId = null;
        if (resetFormulier)
            ResetFormToewijzing();
    }

    private void ActiveerPaneelWerkruimte(Guid? wandId, bool toonEditor = false)
    {
        geopendeWandId = wandId;
        toonEditorDrawer = toonEditor;
    }

    private void SluitPaneelWerklaag()
    {
        toonEditorDrawer = false;
        toonKastOpdelenModal = false;
    }

    private void ResetPaneelInvoer(bool wisSelectie = false)
    {
        VerlaatPaneelBewerkmodus(resetFormulier: true);
        if (wisSelectie)
            WisGeselecteerdeKasten();
        ResetConceptPaneel();
    }

    private void StartPaneelBewerking(PaneelToewijzing toewijzing)
    {
        ActiveerPaneelWerkruimte(VindWandIdVoorToewijzing(toewijzing), toonEditor: true);
        bewerkToewijzingId = toewijzing.Id;
        StelGeselecteerdeKastenIn(toewijzing.KastIds);
        ResetConceptPaneel();
    }
}
