namespace Keuken_inmeten.Services;

public static class TerminologieHelper
{
    public sealed record TerminologieTerm(
        string Sleutel,
        string Label,
        string TechnischeTerm,
        string Uitleg,
        string WanneerRelevant,
        string Voorbeeld,
        string WatNuDoen);

    private static readonly IReadOnlyDictionary<string, TerminologieTerm> Termen = new Dictionary<string, TerminologieTerm>
    {
        ["plinthoogte"] = new(
            "plinthoogte",
            "Hoogte van de plint",
            "plinthoogte",
            "De plint is de strook onder de kasten. Deze maat bepaalt hoe hoog de kasten of apparaten boven de vloer starten.",
            "Deze maat maakt vooral uit als onderkasten, vaatwassers of zijpanelen niet direct op de vloer starten en de onderlijn overal gelijk moet blijven.",
            "Een plinthoogte van 100 mm betekent dat de kastromp 100 mm boven de vloer begint.",
            "Gebruik de standaard van uw keukenlijn als uitgangspunt en wijzig deze alleen als de echte plint hoger of lager wordt."),
        ["wanddikte"] = new(
            "wanddikte",
            "Dikte van de kastwand",
            "wanddikte",
            "De dikte van de zijplaat van de kast. Die beïnvloedt onder meer de positie van systeemgaten en scharnieronderdelen.",
            "Deze maat maakt uit zodra u technische kastdetails controleert of afwijkt van de standaardkast van uw leverancier.",
            "Veel kastrompen gebruiken 18 mm zijwanden; een dikkere zijwand verschuift ook de positie van gaten en referenties.",
            "Laat deze waarde op de standaard staan tenzij uw kasttype aantoonbaar een andere wanddikte gebruikt."),
        ["gaatjesrij"] = new(
            "gaatjesrij",
            "Rij systeemgaten",
            "gaatjesrij",
            "De vaste rij gaten in de kastzijwand. Van daaruit bepaal of controleer je waar de scharnierplaat komt.",
            "Deze term maakt uit als u scharnierplaten, planken of technische kastinstellingen controleert die aan de systeemgaten gekoppeld zijn.",
            "Bij veel keukenkasten is de gaatjesrij een 32 mm systeem dat herhaalbare montageposities geeft.",
            "Controleer de gaatjesrij alleen als uw kast technisch afwijkt of als u in verificatie de scharnierplaatpositie wilt nalopen."),
        ["eerstegat"] = new(
            "eerstegat",
            "Start van de gaatjesrij",
            "eerste gat",
            "Dit is de afstand van de onderkant van de bovenste plank tot het eerste systeemgat in de kastzijwand.",
            "Deze maat maakt vooral uit als de kast niet de standaard booropbouw gebruikt en u wilt weten waar de gatenrij echt begint.",
            "Bij veel kasten is dit 19 mm vanaf de onderkant van de bovenste plank naar het eerste gat.",
            "Laat deze maat op de standaard staan en pas hem alleen aan als uw leverancier of bestaande kast een andere startpositie gebruikt."),
        ["openingsmaat"] = new(
            "openingsmaat",
            "Ruimte waar het paneel in moet passen",
            "openingsmaat",
            "De vrije opening van de kast of het vak voordat er speling van de randen is afgetrokken.",
            "Deze maat maakt uit zodra u een echte opening naloopt of wilt begrijpen waarom de te bestellen paneelmaat kleiner uitvalt.",
            "Een opening van 597 × 717 mm is de bruto ruimte waar het paneel binnen moet passen.",
            "Meet in twijfelgevallen eerst de echte opening en gebruik die maat pas daarna als basis voor het paneel."),
        ["werkmaat"] = new(
            "werkmaat",
            "Netto paneelmaat",
            "werkmaat",
            "De uiteindelijke maat van het paneel nadat vrije ruimte langs de randen is meegerekend.",
            "Deze maat maakt uit zodra u de maat om te bestellen of te zagen wilt afleiden uit de opening en de gekozen randspeling.",
            "Een opening van 597 mm met 2 mm aftrek links en 1 mm rechts wordt 594 mm werkmaat.",
            "Gebruik de werkmaat als bestel- of zaagmaat en niet de ruwe opening als u vrije loop nodig hebt."),
        ["randspeling"] = new(
            "randspeling",
            "Totale voeg tussen rakende delen",
            "randspeling",
            "De totale vrije ruimte tussen twee rakende panelen of tussen een paneel en een apparaat. Vrije buitenranden en gewone kastvlakken krijgen geen aftrek.",
            "Randspeling maakt uit langs randen waar een buurpaneel of apparaat bewegingsruimte vraagt.",
            "Een totale voeg van 3 mm tussen twee panelen kan bijvoorbeeld 1 mm op het ene paneel en 2 mm op het andere worden, terwijl een vrije buitenrand 0 mm aftrek houdt.",
            "Begin met de standaardwaarde en pas alleen aan als het paneel strakker of juist ruimer moet vallen."),
        ["scharnierzijde"] = new(
            "scharnierzijde",
            "Draairichting van de deur",
            "scharnierzijde",
            "De kant waar de scharnieren zitten: links of rechts.",
            "Deze keuze maakt uit zodra een deur moet openzwaaien zonder muur, apparaat of naastliggend paneel te raken.",
            "Scharnieren links betekent meestal dat de greep of trekzijde rechts komt.",
            "Kies de zijde waarop de deur praktisch moet openen en controleer daarna of de vrije ruimte rondom klopt."),
        ["scharnierpot"] = new(
            "scharnierpot",
            "35 mm potscharnier",
            "potscharnier 35 mm",
            "Het ronde 35 mm potscharniergat in het deurpaneel waarin het scharnier valt.",
            "Deze term maakt uit bij deurpanelen met scharnieren; fronten zonder scharnieren hebben deze boring niet nodig.",
            "Een standaard Europese keukendeur gebruikt meestal een 35 mm scharnierpot.",
            "Controleer dit vooral wanneer u technische 35 mm potscharniergaten controleert of productiegegevens doorgeeft voor deurpanelen."),
        ["pothart"] = new(
            "pothart",
            "Afstand hart scharnierpot tot rand",
            "pot-hart van rand",
            "De afstand van het midden van het 35 mm potgat tot de paneelrand aan de scharnierkant.",
            "Deze maat maakt uit als u de deurboring wilt laten aansluiten op het gekozen scharniersysteem en de kastopbouw.",
            "Voor veel Europese scharnieren is 22,5 mm vanaf de rand een gangbare startwaarde.",
            "Gebruik de standaard of laatst gebruikte waarde zolang het scharniersysteem gelijk blijft, en wijk alleen af als het beslag dat vraagt."),
        ["montageplaat"] = new(
            "montageplaat",
            "Midden van de scharnierplaat",
            "montageplaat-midden",
            "De middenlijn van de montageplaat op de kastzijwand. Die ligt tussen twee systeemgaten.",
            "Deze term maakt uit wanneer u in de verificatiestap controleert of de scharnierplaat logisch op de gaatjesrij uitkomt.",
            "De plaat kan bijvoorbeeld midden tussen gat 5 en 6 van de systeemgatenrij landen.",
            "Gebruik deze controle alleen als u deurscharnieren technisch wilt nalopen; voor globale maatcontrole is dit niet nodig."),
        ["kantenband"] = new(
            "kantenband",
            "Afwerkband op de rand",
            "kantenband",
            "Een randafwerking op zichtbare paneelranden, vaak als 1 mm kantenband.",
            "Deze term maakt uit wanneer u bestellijst of productie-uitvoer controleert op zichtkanten en afwerking.",
            "Een zichtzijde van een front kan kantenband nodig hebben, terwijl een verborgen rand kaal mag blijven.",
            "Controleer bij orderregels welke randen zichtbaar blijven en zorg dat daar de juiste kantenband voor staat."),
        ["cnc"] = new(
            "cnc",
            "Machinecoördinaten voor CNC",
            "CNC-coördinaten",
            "De X- en Y-maten die een CNC-machine gebruikt om gaten of bewerkingen op het paneel te plaatsen.",
            "Deze term maakt uit zodra u technische details voor productie, nabewerking of machineoverdracht bekijkt.",
            "Een CNC-regel kan aangeven dat een boring op X 37 mm en Y 96 mm vanaf de referentiehoek moet komen.",
            "Laat CNC-details dicht tijdens gewone controle en open ze pas als u de order technisch moet overdragen of narekenen."),
        ["zaagbreedte"] = new(
            "zaagbreedte",
            "Materiaalverlies per zaagsnede",
            "zaagbreedte",
            "De breedte van het zaagblad die bij iedere snede als materiaalverlies meetelt.",
            "Deze maat maakt uit zodra u in het zaagplan wilt weten hoeveel ruimte tussen twee panelen echt verloren gaat.",
            "Een zaagbreedte van 4 mm betekent dat elke snede 4 mm plaatmateriaal opneemt.",
            "Gebruik de werkelijke zaagblad- of zaagstraatbreedte van uw productieproces zodat het zaagplan realistisch blijft.")
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
