namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public enum BestellijstExportType
{
    Pdf,
    Excel
}

public sealed record BestellijstExportOptie(
    BestellijstExportType Type,
    string Label,
    string KorteBeschrijving,
    string WanneerKiezen,
    string ResultaatUitleg,
    string Voorbeeld,
    string BevestigLabel);

public static class BestellijstExportFlowHelper
{
    public static IReadOnlyList<BestellijstExportOptie> Opties { get; } =
    [
        new(
            BestellijstExportType.Pdf,
            "PDF met visualisaties",
            "Een werkplaatsvriendelijk document met regelcodes, bronlocaties, zaagmaten, boorbeeld en paneelvisualisaties.",
            "Kies PDF als u het resultaat wilt bespreken, printen of rechtstreeks wilt doorgeven aan een zaagbedrijf of werkplaats.",
            "De browser downloadt direct een PDF-document met visualisaties, boorbeeld en materiaalinformatie.",
            "Handig voor overdracht aan de werkplaats of een zaagbedrijf waarin traceerbaarheid en boorbeeld belangrijk zijn.",
            "Download PDF"),
        new(
            BestellijstExportType.Excel,
            "Excel alleen lijst",
            "Een spreadsheet met orderregels, metadata, oppervlaktes, bronlocaties en kolommen voor 35 mm potscharniergaten.",
            "Kies Excel als u wilt filteren, sorteren of de lijst verder wilt bewerken.",
            "De browser downloadt direct een spreadsheetbestand naar uw Downloads-map.",
            "Handig voor werkvoorbereiding of nabewerking waarin u per kolom wilt zoeken en sorteren.",
            "Download Excel")
    ];

    public static BestellijstExportOptie Voor(BestellijstExportType type)
        => type switch
        {
            BestellijstExportType.Pdf => Opties[0],
            BestellijstExportType.Excel => Opties[1],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static IReadOnlyList<string> MaakPreviewPunten(BestellijstExportDocument document, BestellijstExportType type)
        => type switch
        {
            BestellijstExportType.Pdf =>
            [
                $"{document.Orderregels} orderregels voor {document.TotaalAantal} panelen en {BestellijstExportFormatter.FormatVierkanteMeter(document.TotaalOppervlakteM2)} totaal oppervlak.",
                $"Materiaal {document.PaneelType} met dikte {BestellijstExportFormatter.FormatDikteLabel(document.DikteMm)}.",
                "PDF zet per orderregel code, bronlocaties, zaagmaat, boorbeeld en visualisatie bij elkaar voor overdracht aan werkplaats of zaagbedrijf.",
                "De tabelkop herhaalt materiaal, dikte en CNC-referentie zodat meerbladige prints zelfstandig leesbaar blijven."
            ],
            BestellijstExportType.Excel =>
            [
                $"{document.Orderregels} orderregels voor {document.TotaalAantal} panelen en {BestellijstExportFormatter.FormatVierkanteMeter(document.TotaalOppervlakteM2)} totaal oppervlak.",
                $"Materiaal {document.PaneelType} met dikte {BestellijstExportFormatter.FormatDikteLabel(document.DikteMm)}.",
                document.MaxBoorgaten > 0
                    ? $"Excel bevat X/Y-kolommen voor 35 mm potscharniergat 1 t/m {document.MaxBoorgaten}, plus oppervlaktes en bronlocaties."
                    : "Excel blijft een compacte lijst zonder extra kolommen voor 35 mm potscharniergaten omdat deze orderregels geen scharnierboringen hebben.",
                "Gebruik dit formaat als u de lijst wilt filteren, sorteren of doorzetten naar werkvoorbereiding."
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
