namespace Keuken_inmeten.Services;

using System.Text;
using Keuken_inmeten.Models;

public static class BestellijstExportService
{
    public const string CncNulpuntLabel = "Nulpunt linksboven";
    public const string CncXAsLabel = "X vanaf links";
    public const string CncYAsLabel = "Y vanaf boven";

    public static BestellijstExportDocument BouwDocument(IReadOnlyList<BestellijstItem> items, string paneelType, string dikteMm, DateTime generatedAt)
    {
        var regels = items.Select((item, index) => BouwRegel(item, index + 1)).ToList();
        return new BestellijstExportDocument(
            Titel: "Bestellijst",
            PaneelType: paneelType,
            DikteMm: dikteMm,
            GeneratedAt: generatedAt,
            CncNulpuntLabel: CncNulpuntLabel,
            CncXAsLabel: CncXAsLabel,
            CncYAsLabel: CncYAsLabel,
            Orderregels: regels.Count,
            TotaalAantal: regels.Sum(regel => regel.Aantal),
            TotaalBoorgaten: regels.Sum(regel => regel.Boorgaten.Count * regel.Aantal),
            MaxBoorgaten: regels.Count == 0 ? 0 : regels.Max(regel => regel.Boorgaten.Count),
            TotaalOppervlakteM2: Math.Round(regels.Sum(regel => regel.TotaleOppervlakteM2), 3),
            Regels: regels);
    }

    public static string BouwExcelXml(IReadOnlyList<BestellijstItem> items, string paneelType, string dikteMm, DateTime generatedAt)
        => BestellijstExcelRenderer.Render(BouwDocument(items, paneelType, dikteMm, generatedAt));

    public static BestellijstPdfPayload BouwPdfPayload(IReadOnlyList<BestellijstItem> items, string paneelType, string dikteMm, DateTime generatedAt)
        => BestellijstPdfPayloadBuilder.Bouw(BouwDocument(items, paneelType, dikteMm, generatedAt));

    private static BestellijstExportRegel BouwRegel(BestellijstItem item, int regelNummer)
    {
        var regelCode = BestellijstExportFormatter.FormatRegelCode(regelNummer);
        var boorgaten = item.Boorgaten
            .Select((boorgat, index) => new BestellijstExportBoorgat(
                index + 1,
                BerekenCncX(item, boorgat),
                BerekenCncY(boorgat)))
            .ToList();
        List<string> bronLocaties = item.BronLocaties.Count > 0
            ? [.. item.BronLocaties]
            : string.IsNullOrWhiteSpace(item.ContextLabel)
                ? []
                : [item.ContextLabel];
        var oppervlaktePerStukM2 = BerekenOppervlakteM2(item.Breedte, item.Hoogte);
        var totaleOppervlakteM2 = BerekenOppervlakteM2(item.Breedte, item.Hoogte, item.Aantal);

        var visual = new BestellijstVisualDocument(
            regelCode,
            item.Resultaat.Breedte,
            item.Resultaat.Hoogte,
            item.Resultaat.ScharnierZijde,
            [.. item.Resultaat.Boorgaten
                .OrderBy(boorgat => boorgat.Y)
                .Select(boorgat => new BestellijstVisualBoorgat(boorgat.X, boorgat.Y, boorgat.Diameter))]);

        return new BestellijstExportRegel(
            regelNummer,
            regelCode,
            item.Naam,
            item.Aantal,
            item.PaneelRolLabel,
            item.ScharnierLabel,
            item.Hoogte,
            item.Breedte,
            oppervlaktePerStukM2,
            totaleOppervlakteM2,
            item.KantenbandLabel,
            item.ContextLabel,
            bronLocaties,
            boorgaten,
            visual);
    }

    public static string MaakBestandsNaam(string prefix, string paneelType, string extensie, DateTime generatedAt)
    {
        var paneelSlug = Slugify(string.IsNullOrWhiteSpace(paneelType) ? "paneel" : paneelType);
        return $"{prefix}-{paneelSlug}-{generatedAt:yyyyMMdd-HHmm}.{extensie.TrimStart('.')}";
    }

    public static string FormatCncAssenSamenvatting() => BestellijstExportFormatter.FormatCncAssenSamenvatting();

    public static double BerekenCncX(BestellijstItem item, Boorgat boorgat)
    {
        var breedte = item.Resultaat.Breedte > 0 ? item.Resultaat.Breedte : item.Breedte;
        var xVanafLinks = item.Resultaat.ScharnierZijde == ScharnierZijde.Rechts
            ? breedte - boorgat.X
            : boorgat.X;

        return Math.Round(Math.Max(0, xVanafLinks), 1);
    }

    public static double BerekenCncY(Boorgat boorgat) => Math.Round(boorgat.Y, 1);

    public static string FormatDikteLabel(string dikteMm) => BestellijstExportFormatter.FormatDikteLabel(dikteMm);

    public static string FormatMm(double value) => BestellijstExportFormatter.FormatMm(value);

    public static string FormatZaagmaat(double breedteMm, double hoogteMm)
        => BestellijstExportFormatter.FormatZaagmaat(breedteMm, hoogteMm);

    public static string FormatVierkanteMeter(double value)
        => BestellijstExportFormatter.FormatVierkanteMeter(value);

    private static string Slugify(string value)
    {
        var slug = new StringBuilder();
        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                slug.Append(character);
            }
            else if (slug.Length == 0 || slug[^1] == '-')
            {
                continue;
            }
            else
            {
                slug.Append('-');
            }
        }

        return slug.ToString().Trim('-');
    }

    private static double BerekenOppervlakteM2(double breedteMm, double hoogteMm, int aantal = 1)
        => Math.Round((breedteMm * hoogteMm * Math.Max(aantal, 0)) / 1_000_000d, 3);
}
