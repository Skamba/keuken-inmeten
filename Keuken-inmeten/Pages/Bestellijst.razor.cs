using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Keuken_inmeten.Services.Interop;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Keuken_inmeten.Pages;

public partial class Bestellijst
{
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
    private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web);

    private BestellijstExportJsInterop? _exportInterop;
    private BestellijstExportFlowState _exportFlow = BestellijstReadModelHelper.MaakStandaardExportFlow();
    private bool _toonTechnischeDetails;

    private BestellijstExportJsInterop ExportInterop => _exportInterop ??= new(JS);

    private BestellijstPaginaModel MaakPaginaModel(DateTime? generatedAt = null)
        => BestellijstReadModelHelper.BouwPaginaModel(State, _exportFlow, generatedAt ?? DateTime.Now);

    private void OpenExportFlow()
        => _exportFlow = BestellijstReadModelHelper.OpenExportFlow(_exportFlow);

    private void SluitExportFlow()
        => _exportFlow = BestellijstReadModelHelper.SluitExportFlow(_exportFlow);

    private void KiesExportType(BestellijstExportType type)
        => _exportFlow = BestellijstReadModelHelper.KiesExportType(_exportFlow, type);

    private void GaNaarExportPreview()
        => _exportFlow = BestellijstReadModelHelper.GaNaarPreview(_exportFlow);

    private void GaNaarExportBevestiging()
        => _exportFlow = BestellijstReadModelHelper.GaNaarBevestiging(_exportFlow);

    private void GaTerugInExportFlow()
        => _exportFlow = BestellijstReadModelHelper.GaTerug(_exportFlow);

    private void AnnuleerOfGaTerugInExportFlow()
    {
        if (_exportFlow.Stap is BestellijstExportFlowStap.Kiezen)
        {
            SluitExportFlow();
            return;
        }

        GaTerugInExportFlow();
    }

    private async Task BevestigExport()
    {
        if (_exportFlow.ExportType is BestellijstExportType.Excel)
        {
            await ExporteerExcel();
            return;
        }

        if (_exportFlow.ExportType is BestellijstExportType.AdZaagtExcel)
        {
            await ExporteerAdZaagtExcel();
            return;
        }

        await ExporteerPdf();
    }

    private string ExportStapClass(BestellijstExportFlowStap stap)
    {
        var huidigeStap = (int)_exportFlow.Stap;
        var stapNummer = (int)stap;

        return stapNummer == huidigeStap
            ? "bestellijst-export-stap is-active"
            : stapNummer < huidigeStap
                ? "bestellijst-export-stap is-complete"
                : "bestellijst-export-stap";
    }

    private static string ExportTypeTestId(BestellijstExportType type)
        => type switch
        {
            BestellijstExportType.Pdf => "bestellijst-export-type-pdf",
            BestellijstExportType.Excel => "bestellijst-export-type-excel",
            BestellijstExportType.AdZaagtExcel => "bestellijst-export-type-adzaagt",
            _ => "bestellijst-export-type-unknown"
        };

    private async Task ExporteerExcel()
    {
        var generatedAt = DateTime.Now;
        var pagina = MaakPaginaModel(generatedAt);
        var bestand = BestellijstExportService.MaakBestandsNaam("bestellijst", pagina.ExportDocument.PaneelType, "xls", generatedAt);
        var xml = BestellijstExcelRenderer.Render(pagina.ExportDocument);

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

    private async Task ExporteerAdZaagtExcel()
    {
        var generatedAt = DateTime.Now;
        var pagina = MaakPaginaModel(generatedAt);
        var bestand = BestellijstExportService.MaakBestandsNaam("adzaagt", pagina.ExportDocument.PaneelType, "xls", generatedAt);
        var xml = AdZaagtExcelRenderer.Render(pagina.ExportDocument);

        try
        {
            await ExportInterop.DownloadTextFileAsync(
                bestand,
                xml,
                "application/vnd.ms-excel;charset=utf-8");
        }
        catch (JSException)
        {
            Feedback.ToonFout("AdZaagt-export lukte niet. Probeer opnieuw; als er niets gebeurt, controleer of downloads in deze browser zijn toegestaan.");
            return;
        }

        SluitExportFlow();
        Feedback.ToonSucces("AdZaagt-bestand gedownload. Controleer nu de Downloads-map of open het bestand direct.");
    }

    private async Task ExporteerPdf()
    {
        var generatedAt = DateTime.Now;
        var pagina = MaakPaginaModel(generatedAt);
        var bestand = BestellijstExportService.MaakBestandsNaam("bestellijst", pagina.ExportDocument.PaneelType, "pdf", generatedAt);
        var payloadJson = JsonSerializer.Serialize(BestellijstPdfPayloadBuilder.Bouw(pagina.ExportDocument), ExportJsonOptions);

        try
        {
            await ExportInterop.DownloadPdfDocumentAsync(bestand, payloadJson);
        }
        catch (JSException)
        {
            Feedback.ToonFout("PDF-export lukte niet. Probeer opnieuw; als er niets gebeurt, controleer of downloads in deze browser zijn toegestaan.");
            return;
        }

        SluitExportFlow();
        Feedback.ToonSucces("PDF-bestand gedownload. Controleer nu de Downloads-map of open het bestand direct.");
    }

    private string DisplayPaneelType()
        => BestellijstReadModelHelper.BepaalPaneelTypeLabel(_exportFlow.PaneelType);

    private static string ExportKicker(BestellijstExportType type) => type switch
    {
        BestellijstExportType.Pdf => "Rustig document",
        BestellijstExportType.Excel => "Filterbare lijst",
        BestellijstExportType.AdZaagtExcel => "AdZaagt zaagstaat",
        _ => ""
    };

    private static string ExportBadge1(BestellijstExportType type) => type switch
    {
        BestellijstExportType.Pdf => "Download / bespreek",
        BestellijstExportType.Excel => "Filter / sorteer",
        BestellijstExportType.AdZaagtExcel => "Direct insturen",
        _ => ""
    };

    private static string ExportBadge2(BestellijstExportType type) => type switch
    {
        BestellijstExportType.Pdf => "Visualisaties",
        BestellijstExportType.Excel => "Spreadsheet",
        BestellijstExportType.AdZaagtExcel => "AdZaagt-formaat",
        _ => ""
    };

    private string PaneelTypeInput
    {
        get => _exportFlow.PaneelType;
        set => _exportFlow = BestellijstReadModelHelper.StelPaneelTypeIn(_exportFlow, value);
    }

    private string DikteInput
    {
        get => _exportFlow.DikteMm;
        set => _exportFlow = BestellijstReadModelHelper.StelDikteIn(_exportFlow, value);
    }

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
}
