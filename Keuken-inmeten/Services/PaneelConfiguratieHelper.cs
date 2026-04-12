namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class PaneelConfiguratieHelper
{
}

public sealed record PaneelFlowStap(string Id, string Titel, string Beschrijving);

public readonly record struct PaneelOpdeelAnalyse(
    double BeschikbareHoogte,
    double TotaalHoogte,
    double RestantHoogte,
    bool HeeftGeldigeDeelHoogtes,
    bool HeeftGeldigeSom)
{
    public bool KanBevestigen => HeeftGeldigeDeelHoogtes && HeeftGeldigeSom;
}

public readonly record struct PaneelFlowContext(
    bool HeeftWandContext,
    bool HeeftSelectie,
    bool HeeftConceptPaneel,
    bool HeeftGeldigeMaat,
    bool RaaktGeselecteerdeKast,
    bool HeeftConflicterendPaneel,
    string ActieveWandNaam,
    string GeselecteerdeKastNamen);
