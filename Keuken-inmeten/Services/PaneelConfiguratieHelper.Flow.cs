namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class PaneelConfiguratieHelper
{
    public static IReadOnlyList<PaneelFlowStap> PaneelFlowStappen { get; } =
    [
        new("wand", "1. Kies wand", "Open precies een wand als actieve werkruimte."),
        new("selecteren", "2. Selecteer kast(en)", "Kies een of meer kasten binnen die actieve wand."),
        new("plaatsen", "3. Plaats of pas paneel aan", "Sleep het paneel of kies een vrij vak in het schema."),
        new("opslaan", "4. Controleer en sla op", "Check maat en type en bewaar daarna het paneel.")
    ];

    public static bool KanPaneelOpslaan(PaneelFlowContext context)
        => context.HeeftWandContext
            && context.HeeftSelectie
            && context.HeeftConceptPaneel
            && context.HeeftGeldigeMaat
            && context.RaaktGeselecteerdeKast
            && !context.HeeftConflicterendPaneel;

    public static string BepaalVolgendePaneelStapTekst(PaneelFlowContext context)
        => !context.HeeftWandContext
            ? "Open eerst een wand om de paneel-editor te starten."
            : !context.HeeftSelectie
                ? "Selecteer eerst een of meer kasten binnen de actieve wand."
                : !context.HeeftConceptPaneel
                    ? "Plaats of pas het paneel aan in het schema."
                    : context.HeeftConflicterendPaneel
                        ? "Verplaats of verklein het paneel totdat het geen bestaand paneel meer overlapt."
                        : KanPaneelOpslaan(context)
                            ? "Sla het paneel op zodra maat en type kloppen."
                            : "Controleer maat, type en plaatsing zodat het paneel weer een kast raakt.";

    public static string BepaalOpslaanStatusTekst(PaneelFlowContext context)
    {
        if (!context.HeeftWandContext)
            return "Nee, er is nog geen actieve wand.";

        if (!context.HeeftSelectie)
            return "Nee, er is nog geen kastselectie.";

        if (!context.HeeftConceptPaneel)
            return "Nee, er is nog geen paneelpositie.";

        if (!context.HeeftGeldigeMaat)
            return "Nee, breedte en hoogte moeten groter dan 0 zijn.";

        if (context.HeeftConflicterendPaneel)
            return "Nee, het paneel overlapt nog een bestaand paneel.";

        return context.RaaktGeselecteerdeKast
            ? "Ja, u kunt nu opslaan."
            : "Nee, het paneel moet minimaal een geselecteerde kast raken.";
    }

    public static string BepaalGeselecteerdePaneelStatusTekst(PaneelFlowContext context)
    {
        if (!context.HeeftWandContext)
            return "Nog geen wand geopend.";

        return !context.HeeftSelectie
            ? $"{context.ActieveWandNaam} · nog geen kasten geselecteerd."
            : $"{context.ActieveWandNaam} · {context.GeselecteerdeKastNamen}";
    }

    public static string BepaalPaneelFlowStatus(string stapId, PaneelFlowContext context)
        => stapId switch
        {
            "wand" => context.HeeftWandContext ? "done" : "active",
            "selecteren" => !context.HeeftWandContext ? "todo" : context.HeeftSelectie ? "done" : "active",
            "plaatsen" => !context.HeeftWandContext || !context.HeeftSelectie ? "todo" : context.HeeftConceptPaneel ? "done" : "active",
            "opslaan" => !context.HeeftWandContext || !context.HeeftSelectie || !context.HeeftConceptPaneel ? "todo" : "active",
            _ => "todo"
        };

    public static string PaneelFlowContainerClass(string status)
        => status switch
        {
            "done" => "border-success bg-success bg-opacity-10",
            "active" => "border-primary bg-primary bg-opacity-10",
            _ => "bg-light"
        };

    public static string PaneelFlowBadgeClass(string status)
        => status switch
        {
            "done" => "bg-success",
            "active" => "bg-primary",
            _ => "bg-secondary"
        };

    public static string PaneelFlowLabel(string status)
        => status switch
        {
            "done" => "Klaar",
            "active" => "Nu",
            _ => "Wacht"
        };

    public static string TypeBadgeClass(PaneelType type)
        => type switch
        {
            PaneelType.Deur => "bg-primary",
            PaneelType.LadeFront => "bg-warning text-dark",
            _ => "bg-secondary"
        };
}
