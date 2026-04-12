namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class BestellijstPdfPayloadBuilder
{
    public static BestellijstPdfPayload Bouw(BestellijstExportDocument document)
    {
        var materiaalLabel = $"{document.PaneelType} · {BestellijstExportFormatter.FormatDikteLabel(document.DikteMm)}";

        return new BestellijstPdfPayload(
            Titel: document.Titel,
            GeneratedAtLabel: BestellijstExportFormatter.FormatGeneratedAt(document.GeneratedAt),
            PaneelType: document.PaneelType,
            DikteLabel: BestellijstExportFormatter.FormatDikteLabel(document.DikteMm),
            CncReferentieLabel: BestellijstExportFormatter.FormatCncAssenSamenvatting(),
            TotaalOppervlakteLabel: BestellijstExportFormatter.FormatVierkanteMeter(document.TotaalOppervlakteM2),
            Orderregels: document.Orderregels,
            TotaalAantal: document.TotaalAantal,
            TotaalBoorgaten: document.TotaalBoorgaten,
            Regels: document.Regels.Select(regel => BouwRegel(document, regel, materiaalLabel)).ToList());
    }

    private static BestellijstPdfRegelPayload BouwRegel(BestellijstExportDocument document, BestellijstExportRegel regel, string materiaalLabel)
        => new(
            RegelNummer: regel.RegelNummer,
            RegelCode: regel.RegelCode,
            Naam: regel.Naam,
            Aantal: regel.Aantal,
            PaneelMeta: $"{regel.PaneelRolLabel} · {regel.KantenbandLabel}{BuildScharnierMeta(regel)}",
            BronLocaties: BuildBronLocaties(regel),
            ZaagmaatLabel: BestellijstExportFormatter.FormatZaagmaat(regel.BreedteMm, regel.HoogteMm),
            OppervlaktePerStukLabel: BestellijstExportFormatter.FormatVierkanteMeter(regel.OppervlaktePerStukM2),
            TotaleOppervlakteLabel: BestellijstExportFormatter.FormatVierkanteMeter(regel.TotaleOppervlakteM2),
            MateriaalLabel: materiaalLabel,
            BoorbeeldSamenvatting: BestellijstExportFormatter.FormatBoorbeeldSamenvatting(regel.ScharnierLabel, regel.Boorgaten.Count),
            CncReferentieLabel: BestellijstExportFormatter.FormatCncAssenSamenvatting(),
            GeenBoorwerkTekst: "Geen boorwerk voor deze orderregel.",
            Boorgaten: regel.Boorgaten.Select(boorgat => new BestellijstPdfBoorgatPayload(
                boorgat.Nummer,
                BestellijstExportFormatter.FormatMm(boorgat.XCncMm),
                BestellijstExportFormatter.FormatMm(boorgat.YCncMm))).ToList(),
            VisualSvg: BestellijstVisualRenderer.Render(regel.Visual));

    private static IReadOnlyList<string> BuildBronLocaties(BestellijstExportRegel regel)
    {
        if (regel.BronLocaties.Count > 0)
            return regel.BronLocaties;

        if (!string.IsNullOrWhiteSpace(regel.ContextLabel))
            return [regel.ContextLabel];

        return ["—"];
    }

    private static string BuildScharnierMeta(BestellijstExportRegel regel)
        => regel.Boorgaten.Count == 0 || string.IsNullOrWhiteSpace(regel.ScharnierLabel) || regel.ScharnierLabel == "—"
            ? string.Empty
            : $" · scharnier {regel.ScharnierLabel.ToLowerInvariant()}";
}
