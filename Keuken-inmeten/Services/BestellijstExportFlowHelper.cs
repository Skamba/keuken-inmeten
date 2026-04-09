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
            "Een rustig document met orderregels, boorgaten en paneelvisualisaties.",
            "Kies PDF als u het resultaat wilt bespreken, printen of bewaren als leesbaar document.",
            "De browser opent eerst een printweergave; van daaruit kiest u 'Opslaan als PDF'.",
            "Handig voor overdracht aan de werkplaats of een klantbespreking waarin visualisaties belangrijk zijn.",
            "Open printweergave"),
        new(
            BestellijstExportType.Excel,
            "Excel alleen lijst",
            "Een spreadsheet met orderregels, metadata en boorgatkolommen.",
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
                $"{document.Orderregels} orderregels voor {document.TotaalAantal} panelen.",
                $"Materiaal {document.PaneelType} met dikte {BestellijstExportFormatter.FormatDikteLabel(document.DikteMm)}.",
                "PDF voegt per orderregel een visualisatie en boorgatoverzicht toe voor rustige controle of afdrukken.",
                "Gebruik dit formaat als u een leesbaar document wilt delen of als PDF wilt bewaren."
            ],
            BestellijstExportType.Excel =>
            [
                $"{document.Orderregels} orderregels voor {document.TotaalAantal} panelen.",
                $"Materiaal {document.PaneelType} met dikte {BestellijstExportFormatter.FormatDikteLabel(document.DikteMm)}.",
                document.MaxBoorgaten > 0
                    ? $"Excel bevat tabelkolommen tot en met B{document.MaxBoorgaten} X/Y voor boorgatcontrole en verdere bewerking."
                    : "Excel blijft een compacte lijst zonder extra boorgatkolommen omdat deze orderregels geen boorgaten hebben.",
                "Gebruik dit formaat als u de lijst wilt filteren, sorteren of doorzetten naar productie."
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
