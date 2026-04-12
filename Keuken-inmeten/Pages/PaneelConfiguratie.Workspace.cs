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

        reviewWeergaveActief = false;
        geopendeWandId = wandId;
        toonEditorDrawer = true;
        bewerkToewijzingId = null;

        if (ActieveWandId is Guid actieveWandId && actieveWandId != wandId)
            geselecteerdeKastIds.Clear();

        if (!geselecteerdeKastIds.Add(kastId))
            geselecteerdeKastIds.Remove(kastId);

        ResetConceptPaneel();
    }

    private void DeselecteerWand(Guid wandId)
    {
        bewerkToewijzingId = null;
        if (geopendeWandId == wandId)
        {
            reviewWeergaveActief = false;
            geopendeWandId = null;
            toonEditorDrawer = false;
        }

        var wandKastIds = State.KastenVoorWand(wandId).Select(k => k.Id).ToList();
        foreach (var id in wandKastIds)
            geselecteerdeKastIds.Remove(id);

        ResetFormToewijzing();
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
    {
        bewerkToewijzingId = null;
        geselecteerdeKastIds.Clear();
        ResetFormToewijzing();
        ResetConceptPaneel();
    }

    private void OpenWandWerkruimte(Guid wandId)
    {
        if (geopendeWandId != wandId)
        {
            bewerkToewijzingId = null;
            geselecteerdeKastIds.Clear();
            ResetFormToewijzing();
        }

        reviewWeergaveActief = false;
        geopendeWandId = wandId;
        toonEditorDrawer = false;
        ResetConceptPaneel();
    }

    private void SluitWandWerkruimte()
    {
        geopendeWandId = null;
        toonEditorDrawer = false;
        bewerkToewijzingId = null;
        geselecteerdeKastIds.Clear();
        ResetFormToewijzing();
        ResetConceptPaneel();
    }

    private void ActiveerEditorWeergave() => reviewWeergaveActief = false;

    private void ActiveerReviewWeergave()
    {
        if (State.Toewijzingen.Count == 0)
            return;

        reviewWeergaveActief = true;
        toonEditorDrawer = false;
        toonKastOpdelenModal = false;
    }

    private void OpenEditorDrawer()
    {
        if (geopendeWandId is not null)
        {
            reviewWeergaveActief = false;
            toonEditorDrawer = true;
        }
    }

    private void SluitEditorDrawer()
    {
        toonEditorDrawer = false;
        bewerkToewijzingId = null;
        ResetFormToewijzing();
        ResetConceptPaneel();
    }

    private string EditorDrawerTitel()
        => IsBewerkModus
            ? $"Paneel bewerken — {GeopendeWand?.Naam}"
            : ToonCompacteEditorLeegstaat
                ? $"Kastselectie — {GeopendeWand?.Naam}"
                : $"Paneel plaatsen — {GeopendeWand?.Naam}";

    private string EditorWerklaagStatusTekst()
    {
        if (toonEditorDrawer)
            return ToonCompacteEditorLeegstaat ? "Editor open; selecteer nu kast(en)" : "Paneel-editor staat open";

        return geselecteerdeKastIds.Count > 0
            ? "Selectie klaar; open nu de editor"
            : "Selecteer eerst kast(en) in de tekening";
    }

    private string EditorStatusHintTekst()
        => toonEditorDrawer
            ? ToonCompacteEditorLeegstaat
                ? "Zodra u kast(en) kiest, verschijnen plaatsing, maat en opslaan hier."
                : "Plaatsing, maat en opslaan staan nu in de editorlaag."
            : geselecteerdeKastIds.Count > 0
                ? "Open de editor voor plaatsing, maat en opslaan."
                : "Selecteer eerst kast(en) in de tekening.";

    private string OpenEditorKnopLabel()
        => geselecteerdeKastIds.Count > 0 || IsBewerkModus ? "Open paneel-editor" : "Open editorlaag";

    private string PaneelWerkruimteStatusDetailTekst()
        => geselecteerdeKastIds.Count > 0
            ? $"{geselecteerdeKastIds.Count} kast(en) geselecteerd. {EditorStatusHintTekst()}"
            : EditorStatusHintTekst();

    private bool KanKastOpdelenIn(int aantal)
        => OpdeelBereik is { Hoogte: var hoogte }
           && hoogte >= (aantal * PaneelLayoutService.MinPaneelMaat) - 0.001;

    private string OpdeelInstellingenTekst()
        => formToewijzing.Type == PaneelType.Deur
            ? $"{TypeNaam(formToewijzing.Type)} · scharnier {formToewijzing.ScharnierZijde.ToString().ToLowerInvariant()} · pot-hart {FormatMm(PotHartInput)}"
            : TypeNaam(formToewijzing.Type);

    private string OpdeelStatusTekst()
    {
        if (!OpdeelAnalyse.HeeftGeldigeDeelHoogtes)
            return $"Elk deel moet minimaal {FormatMm(PaneelLayoutService.MinPaneelMaat)} hoog zijn.";

        if (OpdeelAnalyse.KanBevestigen)
            return "De ingevulde hoogtes vullen de volledige opening.";

        return OpdeelAnalyse.RestantHoogte > 0
            ? $"Nog {FormatMm(OpdeelAnalyse.RestantHoogte)} te verdelen."
            : $"{FormatMm(Math.Abs(OpdeelAnalyse.RestantHoogte))} te veel ingevuld.";
    }

    private string OpdeelStatusClass()
        => OpdeelAnalyse.KanBevestigen
            ? "alert-success"
            : OpdeelAnalyse.HeeftGeldigeDeelHoogtes
                ? "alert-warning"
                : "alert-danger";

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
        if (!KanKastOpdelenIn(aantal) || OpdeelBereik is not { } bereik)
            return;

        opdeelAantal = aantal;
        opdeelHoogtes = MaakStandaardOpdeelHoogtes(bereik.Hoogte, aantal);
    }

    private void BevestigKastOpdelen()
    {
        if (OpdeelBereik is not { } bereik || geselecteerdeKasten.Count != 1)
            return;

        var analyse = PaneelConfiguratieHelper.AnalyseerOpdeelHoogtes(bereik.Hoogte, opdeelHoogtes);
        if (!analyse.KanBevestigen)
            return;

        var kast = geselecteerdeKasten[0];
        var toewijzingen = PaneelConfiguratieHelper.BouwOpdeelSegmenten(bereik, opdeelHoogtes)
            .Select(segment => MaakPaneelToewijzing(segment, [kast]))
            .ToList();

        State.VoegToewijzingenToe(toewijzingen);
        Feedback.ToonSucces($"{toewijzingen.Count} panelen toegevoegd voor '{kast.Naam}'.");
        RondPaneelInvoerAf();
    }
}
