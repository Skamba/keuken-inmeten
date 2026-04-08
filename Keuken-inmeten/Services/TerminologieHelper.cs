namespace Keuken_inmeten.Services;

public static class TerminologieHelper
{
    public sealed record TerminologieTerm(string Sleutel, string Label, string TechnischeTerm, string Uitleg);

    private static readonly IReadOnlyDictionary<string, TerminologieTerm> Termen = new Dictionary<string, TerminologieTerm>
    {
        ["plinthoogte"] = new(
            "plinthoogte",
            "Hoogte van de plint",
            "plinthoogte",
            "De plint is de strook onder de kasten. Deze maat bepaalt hoe hoog de kasten of apparaten boven de vloer starten."),
        ["wanddikte"] = new(
            "wanddikte",
            "Dikte van de kastwand",
            "wanddikte",
            "De dikte van de zijplaat van de kast. Die beïnvloedt onder meer de positie van systeemgaten en scharnieronderdelen."),
        ["gaatjesrij"] = new(
            "gaatjesrij",
            "Rij systeemgaten",
            "gaatjesrij",
            "De vaste rij gaten in de kastzijwand. Van daaruit bepaal of controleer je waar de scharnierplaat komt."),
        ["openingsmaat"] = new(
            "openingsmaat",
            "Ruimte waar het paneel in moet passen",
            "openingsmaat",
            "De vrije opening van de kast of het vak voordat er speling van de randen is afgetrokken."),
        ["werkmaat"] = new(
            "werkmaat",
            "Netto paneelmaat",
            "werkmaat",
            "De uiteindelijke maat van het paneel nadat vrije ruimte langs de randen is meegerekend."),
        ["randspeling"] = new(
            "randspeling",
            "Vrije ruimte langs de rand",
            "randspeling",
            "Een kleine speling zodat het paneel niet klemt. Aan twee kanten 2 mm speling betekent 4 mm minder werkmaat."),
        ["scharnierzijde"] = new(
            "scharnierzijde",
            "Draairichting van de deur",
            "scharnierzijde",
            "De kant waar de scharnieren zitten: links of rechts."),
        ["scharnierpot"] = new(
            "scharnierpot",
            "35 mm scharnierpot",
            "pot 35 mm",
            "Het ronde boorgat in het deurpaneel waarin het scharnier valt."),
        ["pothart"] = new(
            "pothart",
            "Afstand hart scharnierpot tot rand",
            "pot-hart van rand",
            "De afstand van het midden van het 35 mm potgat tot de paneelrand aan de scharnierkant."),
        ["montageplaat"] = new(
            "montageplaat",
            "Midden van de scharnierplaat",
            "montageplaat-midden",
            "De middenlijn van de montageplaat op de kastzijwand. Die ligt tussen twee systeemgaten."),
        ["absband"] = new(
            "absband",
            "Afwerkband op de rand",
            "ABS-band",
            "Een kunststof randafwerking op zichtbare paneelranden."),
        ["cnc"] = new(
            "cnc",
            "Machinecoordinaten voor CNC",
            "CNC-coordinaten",
            "De X- en Y-maten die een CNC-machine gebruikt om gaten of bewerkingen op het paneel te plaatsen."),
        ["zaagbreedte"] = new(
            "zaagbreedte",
            "Materiaalverlies per zaagsnede",
            "zaagbreedte",
            "De breedte van het zaagblad die bij iedere snede als materiaalverlies meetelt.")
    };

    public static TerminologieTerm HaalTermOp(string sleutel)
        => Termen.TryGetValue(sleutel, out var term)
            ? term
            : throw new ArgumentOutOfRangeException(nameof(sleutel), sleutel, "Onbekende terminologiesleutel.");

    public static IReadOnlyList<TerminologieTerm> HaalTermenOp(IEnumerable<string> sleutels)
        => sleutels
            .Where(sleutel => !string.IsNullOrWhiteSpace(sleutel))
            .Select(sleutel => sleutel.Trim().ToLowerInvariant())
            .Distinct()
            .Select(HaalTermOp)
            .ToList();
}
