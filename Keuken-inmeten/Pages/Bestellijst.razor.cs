using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Keuken_inmeten.Services.Interop;
using Microsoft.JSInterop;

namespace Keuken_inmeten.Pages;

public partial class Bestellijst
{
    private enum BestellijstExportFlowStap
    {
        Kiezen = 1,
        Preview = 2,
        Bevestigen = 3
    }

    private static readonly string[] PaneelTypeOpties =
    [
        "MDF gelakt",
        "Melamine",
        "Fenix",
        "HPL",
        "Fineer",
        "Multiplex",
        "Acrylaat"
    ];

    private BestellijstExportJsInterop? _exportInterop;
    private BestellijstExportFlowStap _exportFlowStap = BestellijstExportFlowStap.Kiezen;
    private BestellijstExportType _exportType = BestellijstExportType.Pdf;
    private string _paneelType = "MDF gelakt";
    private string _dikteMm = "19";
    private bool _toonTechnischeDetails;
    private bool _toonExportFlow;

    private BestellijstExportJsInterop ExportInterop => _exportInterop ??= new(JS);

    private BestellijstPaginaModel MaakPaginaModel()
    {
        var flowStatus = StappenFlowHelper.BepaalStatus(State);
        var items = BestellijstService.BerekenItems(State);
        var exportDocument = MaakExportDocument(items, DateTime.Now);

        return new BestellijstPaginaModel(
            RouteGate: StappenFlowHelper.BepaalRouteGate("bestellijst", flowStatus),
            Items: items,
            WandGroepen: OverzichtGroeperingHelper.GroepeerBestellijstOpWand(items),
            PaneelTypeTellingen: OverzichtGroeperingHelper.MaakTellingen(items, item => item.PaneelRolLabel, item => item.Aantal),
            TotaalAantal: items.Sum(item => item.Aantal),
            TotaalBoorgaten: items.Sum(item => item.Aantal * item.Boorgaten.Count),
            ExportDocument: exportDocument,
            GekozenExport: BestellijstExportFlowHelper.Voor(_exportType),
            ExportPreviewPunten: BestellijstExportFlowHelper.MaakPreviewPunten(exportDocument, _exportType));
    }

    private BestellijstExportDocument MaakExportDocument(IReadOnlyList<BestellijstItem> items, DateTime generatedAt)
        => BestellijstExportService.BouwDocument(items, DisplayPaneelType(), _dikteMm, generatedAt);

    private void OpenExportFlow()
    {
        _toonExportFlow = true;
        _exportFlowStap = BestellijstExportFlowStap.Kiezen;
    }

    private void SluitExportFlow()
    {
        _toonExportFlow = false;
        _exportFlowStap = BestellijstExportFlowStap.Kiezen;
    }

    private void KiesExportType(BestellijstExportType type) => _exportType = type;

    private void GaNaarExportPreview() => _exportFlowStap = BestellijstExportFlowStap.Preview;

    private void GaNaarExportBevestiging() => _exportFlowStap = BestellijstExportFlowStap.Bevestigen;

    private void GaTerugInExportFlow()
    {
        _exportFlowStap = _exportFlowStap switch
        {
            BestellijstExportFlowStap.Bevestigen => BestellijstExportFlowStap.Preview,
            _ => BestellijstExportFlowStap.Kiezen
        };
    }

    private void AnnuleerOfGaTerugInExportFlow()
    {
        if (_exportFlowStap is BestellijstExportFlowStap.Kiezen)
        {
            SluitExportFlow();
            return;
        }

        GaTerugInExportFlow();
    }

    private async Task BevestigExport()
    {
        if (_exportType is BestellijstExportType.Excel)
        {
            await ExporteerExcel();
            return;
        }

        await ExporteerPdf();
    }

    private string ExportStapClass(BestellijstExportFlowStap stap)
    {
        var huidigeStap = (int)_exportFlowStap;
        var stapNummer = (int)stap;

        return stapNummer == huidigeStap
            ? "bestellijst-export-stap is-active"
            : stapNummer < huidigeStap
                ? "bestellijst-export-stap is-complete"
                : "bestellijst-export-stap";
    }

    private static string ExportTypeTestId(BestellijstExportType type)
        => type is BestellijstExportType.Pdf
            ? "bestellijst-export-type-pdf"
            : "bestellijst-export-type-excel";

    private async Task ExporteerExcel()
    {
        var items = BestellijstService.BerekenItems(State);
        var generatedAt = DateTime.Now;
        var document = MaakExportDocument(items, generatedAt);
        var bestand = BestellijstExportService.MaakBestandsNaam("bestellijst", DisplayPaneelType(), "xls", generatedAt);
        var xml = BestellijstExcelRenderer.Render(document);

        try
        {
            await ExportInterop.DownloadTextFileAsync(
                bestand,
                xml,
                "application/vnd.ms-excel;charset=utf-8");
        }
        catch (JSException)
        {
            Feedback.ToonFout("Excel-export lukte niet. Probeer opnieuw; als er niets gebeurt, controleer of downloads in deze browser zijn toegestaan.");
            return;
        }

        SluitExportFlow();
        Feedback.ToonSucces("Excel-bestand gedownload. Controleer nu de Downloads-map of open het bestand direct.");
    }

    private async Task ExporteerPdf()
    {
        var items = BestellijstService.BerekenItems(State);
        var document = MaakExportDocument(items, DateTime.Now);
        var html = BestellijstPrintHtmlRenderer.Render(document);

        try
        {
            await ExportInterop.OpenPrintDocumentAsync(html);
        }
        catch (JSException)
        {
            Feedback.ToonFout("PDF-export lukte niet. Controleer of pop-ups zijn toegestaan en probeer opnieuw.");
            return;
        }

        SluitExportFlow();
        Feedback.ToonSucces("Printweergave geopend. Kies in de browser 'Opslaan als PDF' om het bestand te bewaren.");
    }

    private string DisplayPaneelType()
        => string.IsNullOrWhiteSpace(_paneelType) ? "Onbekend paneeltype" : _paneelType.Trim();

    private static string MaatLabel(BestellijstItem item)
        => $"{FormatNumber(item.Hoogte)} hoog × {FormatNumber(item.Breedte)} breed";

    private static string ContextSamenvatting(BestellijstItem item)
    {
        var context = item.ContextLabel?.Trim() ?? string.Empty;
        var prefix = string.IsNullOrWhiteSpace(item.WandNaam) ? string.Empty : $"{item.WandNaam} • ";

        if (!string.IsNullOrWhiteSpace(prefix) && context.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
            context = context[prefix.Length..];

        if (string.IsNullOrWhiteSpace(context))
            return string.IsNullOrWhiteSpace(item.WandNaam) ? "—" : item.WandNaam;

        return context;
    }

    private static List<string> TechnischeDetailLijnen(BestellijstItem item)
    {
        var regels = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.ScharnierLabel) && item.ScharnierLabel != "—")
            regels.Add($"Scharnier: {item.ScharnierLabel}");

        if (item.Boorgaten.Count == 0)
        {
            regels.Add("Geen 35 mm potscharniergaten.");
            return regels;
        }

        for (var i = 0; i < item.Boorgaten.Count; i++)
        {
            var boorgat = item.Boorgaten[i];
            regels.Add(
                $"Potscharniergat {i + 1} (35 mm): X {FormatNumber(BestellijstExportService.BerekenCncX(item, boorgat))} · Y {FormatNumber(BestellijstExportService.BerekenCncY(boorgat))}");
        }

        return regels;
    }

    private static string FormatPotscharnierGatTelling(int aantal)
        => $"{aantal} {(aantal == 1 ? "35 mm potscharniergat" : "35 mm potscharniergaten")}";

    private static string FormatNumber(double value) => value.ToString("0.#");

    public async ValueTask DisposeAsync()
    {
        if (_exportInterop is not null)
            await _exportInterop.DisposeAsync();
    }

    private sealed record BestellijstPaginaModel(
        StapRouteGate? RouteGate,
        List<BestellijstItem> Items,
        List<OverzichtGroeperingHelper.BestellijstWandGroep> WandGroepen,
        List<OverzichtGroeperingHelper.Telling> PaneelTypeTellingen,
        int TotaalAantal,
        int TotaalBoorgaten,
        BestellijstExportDocument ExportDocument,
        BestellijstExportOptie GekozenExport,
        IReadOnlyList<string> ExportPreviewPunten);
}
