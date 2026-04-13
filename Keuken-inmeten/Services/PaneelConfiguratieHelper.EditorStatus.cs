namespace Keuken_inmeten.Services;

public static partial class PaneelConfiguratieHelper
{
    public static PaneelEditorStatusModel BouwEditorStatus(PaneelEditorStatusContext context)
    {
        var wandNaam = string.IsNullOrWhiteSpace(context.GeopendeWandNaam)
            ? string.IsNullOrWhiteSpace(context.Flow.ActieveWandNaam)
                ? "Paneel-editor"
                : context.Flow.ActieveWandNaam
            : context.GeopendeWandNaam;
        var kanOpslaan = KanPaneelOpslaan(context.Flow);
        var hintTekst = BepaalEditorHintTekst(context, kanOpslaan);

        return new PaneelEditorStatusModel(
            GeopendeWandNaam: wandNaam,
            ToonCompacteEditorLeegstaat: context.ToonCompacteEditorLeegstaat,
            IsBewerkModus: context.IsBewerkModus,
            HeeftEnkeleKastSelectie: context.HeeftEnkeleKastSelectie,
            KanKastOpdelen: context.KanKastOpdelen,
            KanOpslaan: kanOpslaan,
            EditorDrawerTitel: context.IsBewerkModus
                ? $"Paneel bewerken — {wandNaam}"
                : context.ToonCompacteEditorLeegstaat
                    ? $"Kastselectie — {wandNaam}"
                    : $"Paneel plaatsen — {wandNaam}",
            EditorHeaderMeta: context.IsBewerkModus
                ? $"Paneel {context.BewerkIndex}"
                : context.GeselecteerdeKastAantal > 0
                    ? $"{context.GeselecteerdeKastAantal} kast(en) geselecteerd"
                    : null,
            VolgendePaneelStapTekst: BepaalVolgendePaneelStapTekst(context.Flow),
            KernHintTekst: BepaalKernHintTekst(context.Flow, kanOpslaan),
            SelectieSamenvatting: BepaalSelectieSamenvatting(context),
            OpslaanSamenvatting: kanOpslaan ? "Klaar" : "Nog controleren",
            OpslaanStatusTekst: BepaalOpslaanStatusTekst(context.Flow),
            WerklaagStatusTekst: context.ToonEditorDrawer
                ? context.ToonCompacteEditorLeegstaat
                    ? "Editor open; selecteer nu kast(en)"
                    : "Paneel-editor staat open"
                : context.GeselecteerdeKastAantal > 0
                    ? "Selectie klaar; open nu de editor"
                    : "Selecteer eerst kast(en) in de tekening",
            WerkruimteStatusDetailTekst: context.GeselecteerdeKastAantal > 0
                ? $"{context.GeselecteerdeKastAantal} kast(en) geselecteerd. {hintTekst}"
                : hintTekst,
            OpenEditorKnopLabel: context.GeselecteerdeKastAantal > 0 || context.IsBewerkModus
                ? "Open paneel-editor"
                : "Open editorlaag",
            OpdeelStatusClass: BepaalOpdeelStatusClass(context.OpdeelAnalyse),
            OpdeelStatusTekst: BepaalOpdeelStatusTekst(context.OpdeelAnalyse));
    }

    private static string BepaalSelectieSamenvatting(PaneelEditorStatusContext context)
        => context.GeselecteerdeKastAantal switch
        {
            0 => "Nog geen kast",
            1 => context.Flow.GeselecteerdeKastNamen,
            _ => $"{context.GeselecteerdeKastAantal} kasten"
        };

    private static string BepaalKernHintTekst(PaneelFlowContext context, bool kanOpslaan)
    {
        if (kanOpslaan)
            return "Controleer hieronder alleen nog maat en type. Daarna kunt u direct opslaan.";

        if (!context.HeeftConceptPaneel)
            return "Sleep in de tekening of kies een vrij vak. De velden hieronder volgen direct mee.";

        if (context.HeeftConflicterendPaneel)
            return "Verplaats of verklein het paneel totdat het geen bestaand paneel meer overlapt.";

        return context.RaaktGeselecteerdeKast
            ? "Controleer hieronder maat en type voordat u opslaat."
            : "Pas positie, maat of selectie aan totdat het paneel weer een geselecteerde kast raakt.";
    }

    private static string BepaalEditorHintTekst(PaneelEditorStatusContext context, bool kanOpslaan)
        => context.ToonEditorDrawer
            ? context.ToonCompacteEditorLeegstaat
                ? "Zodra u kast(en) kiest, verschijnen plaatsing, maat en opslaan hier."
                : "Plaatsing, maat en opslaan staan nu in de editorlaag."
            : context.GeselecteerdeKastAantal > 0
                ? "Open de editor voor plaatsing, maat en opslaan."
                : "Selecteer eerst kast(en) in de tekening.";

    private static string BepaalOpdeelStatusTekst(PaneelOpdeelAnalyse analyse)
    {
        if (!analyse.HeeftGeldigeDeelHoogtes)
            return $"Elk deel moet minimaal {FormatMm(PaneelLayoutService.MinPaneelMaat)} hoog zijn.";

        if (analyse.KanBevestigen)
            return "De ingevulde hoogtes vullen de volledige opening.";

        return analyse.RestantHoogte > 0
            ? $"Nog {FormatMm(analyse.RestantHoogte)} te verdelen."
            : $"{FormatMm(Math.Abs(analyse.RestantHoogte))} te veel ingevuld.";
    }

    private static string BepaalOpdeelStatusClass(PaneelOpdeelAnalyse analyse)
        => analyse.KanBevestigen
            ? "alert-success"
            : analyse.HeeftGeldigeDeelHoogtes
                ? "alert-warning"
                : "alert-danger";

    private static string FormatMm(double waarde) => $"{waarde:0.#} mm";
}

public readonly record struct PaneelEditorStatusContext(
    PaneelFlowContext Flow,
    string GeopendeWandNaam,
    int GeselecteerdeKastAantal,
    bool ToonEditorDrawer,
    bool ToonCompacteEditorLeegstaat,
    bool IsBewerkModus,
    bool HeeftEnkeleKastSelectie,
    bool KanKastOpdelen,
    int BewerkIndex,
    PaneelOpdeelAnalyse OpdeelAnalyse);

public sealed record PaneelEditorStatusModel(
    string GeopendeWandNaam,
    bool ToonCompacteEditorLeegstaat,
    bool IsBewerkModus,
    bool HeeftEnkeleKastSelectie,
    bool KanKastOpdelen,
    bool KanOpslaan,
    string EditorDrawerTitel,
    string? EditorHeaderMeta,
    string VolgendePaneelStapTekst,
    string KernHintTekst,
    string SelectieSamenvatting,
    string OpslaanSamenvatting,
    string OpslaanStatusTekst,
    string WerklaagStatusTekst,
    string WerkruimteStatusDetailTekst,
    string OpenEditorKnopLabel,
    string OpdeelStatusClass,
    string OpdeelStatusTekst);
