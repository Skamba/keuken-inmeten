using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public enum BestellijstExportFlowStap
{
    Kiezen = 1,
    Preview = 2,
    Bevestigen = 3
}

public sealed record BestellijstExportFlowState(
    bool ToonExportFlow,
    BestellijstExportFlowStap Stap,
    BestellijstExportType ExportType,
    string PaneelType,
    string DikteMm);

public sealed record BestellijstPaginaModel(
    StapRouteGate? RouteGate,
    IReadOnlyList<BestellijstItem> Items,
    IReadOnlyList<OverzichtGroeperingHelper.BestellijstWandGroep> WandGroepen,
    IReadOnlyList<OverzichtGroeperingHelper.Telling> PaneelTypeTellingen,
    int TotaalAantal,
    int TotaalBoorgaten,
    BestellijstExportDocument ExportDocument,
    BestellijstExportOptie GekozenExport,
    IReadOnlyList<string> ExportPreviewPunten);

public static class BestellijstReadModelHelper
{
    public static BestellijstExportFlowState MaakStandaardExportFlow()
        => new(
            ToonExportFlow: false,
            Stap: BestellijstExportFlowStap.Kiezen,
            ExportType: BestellijstExportType.Pdf,
            PaneelType: "MDF gelakt",
            DikteMm: "19");

    public static BestellijstExportFlowState OpenExportFlow(BestellijstExportFlowState exportFlow)
        => exportFlow with { ToonExportFlow = true, Stap = BestellijstExportFlowStap.Kiezen };

    public static BestellijstExportFlowState SluitExportFlow(BestellijstExportFlowState exportFlow)
        => exportFlow with { ToonExportFlow = false, Stap = BestellijstExportFlowStap.Kiezen };

    public static BestellijstExportFlowState KiesExportType(BestellijstExportFlowState exportFlow, BestellijstExportType exportType)
        => exportFlow with { ExportType = exportType };

    public static BestellijstExportFlowState GaNaarPreview(BestellijstExportFlowState exportFlow)
        => exportFlow with { Stap = BestellijstExportFlowStap.Preview };

    public static BestellijstExportFlowState GaNaarBevestiging(BestellijstExportFlowState exportFlow)
        => exportFlow with { Stap = BestellijstExportFlowStap.Bevestigen };

    public static BestellijstExportFlowState GaTerug(BestellijstExportFlowState exportFlow)
        => exportFlow with
        {
            Stap = exportFlow.Stap is BestellijstExportFlowStap.Bevestigen
                ? BestellijstExportFlowStap.Preview
                : BestellijstExportFlowStap.Kiezen
        };

    public static BestellijstExportFlowState StelPaneelTypeIn(BestellijstExportFlowState exportFlow, string paneelType)
        => exportFlow with { PaneelType = paneelType };

    public static BestellijstExportFlowState StelDikteIn(BestellijstExportFlowState exportFlow, string dikteMm)
        => exportFlow with { DikteMm = dikteMm };

    public static string BepaalPaneelTypeLabel(string paneelType)
        => string.IsNullOrWhiteSpace(paneelType) ? "Onbekend paneeltype" : paneelType.Trim();

    public static BestellijstPaginaModel BouwPaginaModel(
        KeukenStateService state,
        BestellijstExportFlowState exportFlow,
        DateTime generatedAt)
    {
        var flowStatus = StappenFlowHelper.BepaalStatus(state);
        var items = BestellijstService.BerekenItems(state);
        var paneelTypeLabel = BepaalPaneelTypeLabel(exportFlow.PaneelType);
        var exportDocument = BestellijstExportService.BouwDocument(items, paneelTypeLabel, exportFlow.DikteMm, generatedAt);

        return new BestellijstPaginaModel(
            RouteGate: StappenFlowHelper.BepaalRouteGate("bestellijst", flowStatus),
            Items: items,
            WandGroepen: OverzichtGroeperingHelper.GroepeerBestellijstOpWand(items),
            PaneelTypeTellingen: OverzichtGroeperingHelper.MaakTellingen(items, item => item.PaneelRolLabel, item => item.Aantal),
            TotaalAantal: items.Sum(item => item.Aantal),
            TotaalBoorgaten: items.Sum(item => item.Aantal * item.Boorgaten.Count),
            ExportDocument: exportDocument,
            GekozenExport: BestellijstExportFlowHelper.Voor(exportFlow.ExportType),
            ExportPreviewPunten: BestellijstExportFlowHelper.MaakPreviewPunten(exportDocument, exportFlow.ExportType));
    }
}
