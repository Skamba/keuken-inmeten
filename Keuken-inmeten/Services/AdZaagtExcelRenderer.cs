namespace Keuken_inmeten.Services;

using System.Globalization;
using System.Net;
using System.Text;
using Keuken_inmeten.Models;

public static class AdZaagtExcelRenderer
{
    private const string KantcodeRondom = "1";

    private static readonly string[] KantcodeLegenda =
    [
        "1 = 1mm pvc kleur als basis materiaal",
        "2 = verstek onderkant",
        "3 = verstek bovenkant",
        "4 = schuine kant onderkant 2 mm",
        "5 = schuine kant bovenkant 2 mm",
        "6 = schuine kant onder+boven 2 mm",
        "7 = afronden onderkant radius 2 mm",
        "8 = afronden bovenkant radius 2 mm",
        "9 = afronden onder + boven radius 2 mm",
        "10 = 1 mm kantfineer"
    ];

    public static string Render(BestellijstExportDocument document)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
        sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
        sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        sb.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("  <Styles>");
        sb.AppendLine("    <Style ss:ID=\"Titel\"><Font ss:Bold=\"1\" ss:Size=\"14\" /></Style>");
        sb.AppendLine("    <Style ss:ID=\"Kop\"><Font ss:Bold=\"1\" /><Interior ss:Color=\"#DCE6F0\" ss:Pattern=\"Solid\" /></Style>");
        sb.AppendLine("    <Style ss:ID=\"Meta\"><Font ss:Color=\"#555555\" /></Style>");
        sb.AppendLine("    <Style ss:ID=\"Legenda\"><Font ss:Color=\"#888888\" ss:Size=\"9\" /></Style>");
        sb.AppendLine("  </Styles>");
        sb.AppendLine("  <Worksheet ss:Name=\"AdZaagt\">");
        sb.AppendLine("    <Table>");

        // Row 0: Title (merged across F–M)
        AppendSparseRow(sb, (6, CellText("INVULLIJST ZAAGSTAAT ADZAAGT\nEindmaten invullen in mm, inclusief kantenband", "Titel", mergeAcross: 7)));

        // Row 1: Kantcodes header (merged I–L) + code 1
        AppendSparseRow(sb,
            (9, CellText("Kantcodes:", "Meta", mergeAcross: 3)),
            (13, CellText($"    {KantcodeLegenda[0]}", "Legenda")));

        // Rows 2-3: codes 2-3
        AppendSparseRow(sb, (13, CellText($"    {KantcodeLegenda[1]}", "Legenda")));
        AppendSparseRow(sb, (13, CellText($"    {KantcodeLegenda[2]}", "Legenda")));

        // Row 4: Orderinformatie (merged F–L) + code 4
        AppendSparseRow(sb,
            (6, CellText("Orderinformatie:", "Meta", mergeAcross: 6)),
            (13, CellText($"    {KantcodeLegenda[3]}", "Legenda")));

        // Rows 5-10: Order info labels (merged F–L) + codes 5-10
        string[] orderLabels = ["Naam klant:", "Referentie:", "Adres:", "Postcode:", "Plaats:", "Tel. nummer:"];
        for (var i = 0; i < orderLabels.Length; i++)
        {
            AppendSparseRow(sb,
                (6, CellText(orderLabels[i], "Meta", mergeAcross: 6)),
                (13, CellText($"    {KantcodeLegenda[i + 4]}", "Legenda")));
        }

        // Row 11: empty
        AppendRow(sb, []);

        // Row 12: Header row
        AppendRow(sb,
        [
            CellText("Materiaal", "Kop"),
            CellText("Color", "Kop"),
            CellText("Dikte (mm)", "Kop"),
            CellText("Nerf-richting ja/nee", "Kop"),
            CellText("Aantal", "Kop"),
            CellText("Breedte", "Kop"),
            CellText("(Fineer-richting) Lengte", "Kop"),
            CellText("Onderdeel", "Kop"),
            CellText("BB", "Kop"),
            CellText("BO", "Kop"),
            CellText("LR", "Kop"),
            CellText("LL", "Kop"),
            CellText("Extra bewerkingen", "Kop")
        ]);

        // Data rows
        foreach (var regel in document.Regels)
        {
            var kantcode = BepaalKantcode(regel.KantenbandLabel);
            var extraBewerkingen = BouwExtraBewerkingen(regel.Boorgaten);

            AppendRow(sb,
            [
                CellText(document.PaneelType),
                CellText(""),
                CellNumber(double.TryParse(document.DikteMm, NumberStyles.Any, CultureInfo.InvariantCulture, out var dikte) ? dikte : 0),
                CellText("nee"),
                CellNumber(regel.Aantal),
                CellNumber(regel.BreedteMm),
                CellNumber(regel.HoogteMm),
                CellText(regel.Naam),
                CellText(kantcode),
                CellText(kantcode),
                CellText(kantcode),
                CellText(kantcode),
                CellText(extraBewerkingen)
            ]);
        }

        sb.AppendLine("    </Table>");
        sb.AppendLine("  </Worksheet>");
        sb.AppendLine("</Workbook>");
        return sb.ToString();
    }

    public static string BepaalKantcode(string kantenbandLabel)
        => kantenbandLabel.Contains("rondom", StringComparison.OrdinalIgnoreCase) ? KantcodeRondom : "";

    private static string BouwExtraBewerkingen(IReadOnlyList<BestellijstExportBoorgat> boorgaten)
    {
        if (boorgaten.Count == 0) return "";

        var posities = string.Join(", ", boorgaten
            .OrderBy(b => b.YCncMm)
            .Select(b => $"({Fmt(b.XCncMm)};{Fmt(b.YCncMm)})"));

        return $"Scharnierboringen: {posities}";
    }

    private static void AppendSparseRow(StringBuilder sb, params (int ColumnIndex1Based, string CellXml)[] cells)
    {
        sb.AppendLine("      <Row>");
        foreach (var (columnIndex, cellXml) in cells)
            sb.AppendLine($"        {InjectIndex(cellXml, columnIndex)}");
        sb.AppendLine("      </Row>");
    }

    private static string InjectIndex(string cellXml, int index)
        => cellXml.Insert(5, $" ss:Index=\"{index}\"");

    private static void AppendRow(StringBuilder sb, IEnumerable<string> cells)
    {
        sb.AppendLine("      <Row>");
        foreach (var cell in cells)
            sb.AppendLine($"        {cell}");
        sb.AppendLine("      </Row>");
    }

    private static string CellText(string value, string? styleId = null, int mergeAcross = 0)
    {
        var encoded = Encode(value);
        var mergeAttr = mergeAcross > 0 ? $" ss:MergeAcross=\"{mergeAcross}\"" : "";
        return styleId is null
            ? $"<Cell{mergeAttr}><Data ss:Type=\"String\">{encoded}</Data></Cell>"
            : $"<Cell ss:StyleID=\"{styleId}\"{mergeAttr}><Data ss:Type=\"String\">{encoded}</Data></Cell>";
    }

    private static string CellNumber(double value, string? styleId = null)
    {
        var formatted = Fmt(value);
        return styleId is null
            ? $"<Cell><Data ss:Type=\"Number\">{formatted}</Data></Cell>"
            : $"<Cell ss:StyleID=\"{styleId}\"><Data ss:Type=\"Number\">{formatted}</Data></Cell>";
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
    private static string Fmt(double value) => value.ToString(CultureInfo.InvariantCulture);
}
