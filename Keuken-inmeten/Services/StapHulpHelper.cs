namespace Keuken_inmeten.Services;

public static class StapHulpHelper
{
    public sealed record HulpNiveau(string Id, string Titel, string Gebruik, string Voorbeeld);

    public sealed record StapHulpSectie(string Titel, string Beschrijving, IReadOnlyList<string> Punten);

    public sealed record StapHulpContext(
        string StapId,
        string Titel,
        string Intro,
        IReadOnlyList<HulpNiveau> Niveaus,
        IReadOnlyList<StapHulpSectie> Secties);

    private static readonly HulpNiveau[] StandaardHulpNiveaus =
    [
        new(
            "hint",
            "Korte hint",
            "Gebruik een hint direct onder een veld voor de verwachte maat, eenheid of standaardkeuze.",
            "Bijvoorbeeld: standaard 2 mm randspeling of meestal 18 mm wanddikte."),
        new(
            "info",
            "Info-knop",
            "Gebruik een info-knop voor een korte definitie, een voorbeeld of een kleine beslisregel zonder het formulier langer te maken.",
            "Bijvoorbeeld: wat een gaatjesrij is en wanneer u daarvan afwijkt."),
        new(
            "details",
            "Detailsblok",
            "Gebruik een detailsblok voor optionele extra uitleg of een berekening die alleen soms nodig is.",
            "Bijvoorbeeld: hoe maat in de opening en maat om te bestellen uit elkaar lopen."),
        new(
            "drawer",
            "Verdiepende hulp",
            "Gebruik de zijhulp voor langere stapgerichte uitleg, voorbeelden en beslisregels terwijl de hoofdflow intact blijft.",
            "Bijvoorbeeld: wanneer u CNC-details toont of wanneer een apparaat echt mee moet in de wand.")
    ];

    private static readonly IReadOnlyDictionary<string, StapHulpContext> StapHulp = new Dictionary<string, StapHulpContext>(StringComparer.Ordinal)
    {
        ["kasten"] = new(
            "kasten",
            "Hulp bij stap 1: Indeling",
            "Gebruik deze hulp om te bepalen welke informatie direct in het formulier hoort en welke technische uitleg u alleen op aanvraag nodig hebt.",
            StandaardHulpNiveaus,
            [
                new(
                    "Kasten en apparaten toevoegen",
                    "Stap 1 blijft bewust rustig: voeg eerst de juiste objecten toe en verfijn technische details alleen als de standaard niet klopt.",
                    [
                        "Gebruik de korte hints voor naam, wand en maatverwachtingen.",
                        "Open info-knoppen bij wanddikte, systeemgaten en het eerste gat als u twijfelt over de betekenis van die maat.",
                        "Laat technische instellingen dicht als de kast de standaardopbouw volgt."
                    ]),
                new(
                    "Wanneer hoort een apparaat in de wand?",
                    "Voeg een apparaat alleen toe als het echt ruimte inneemt in de opstelling of de vrije paneelruimte verandert.",
                    [
                        "Een oven, koelkast of vaatwasser hoort in de wand als die tussen kasten staat of maatvoering beïnvloedt.",
                        "Gebruik details of verdiepende hulp als u de impact op panelen of vrije vakken wilt nalopen."
                    ])
            ]),
        ["panelen"] = new(
            "panelen",
            "Hulp bij stap 2: Panelen",
            "In deze stap opent u eerst precies één wand als werkcontext, kiest daarna de juiste kast(en) en past pas daarna maat, richting en technische details van het paneel aan.",
            StandaardHulpNiveaus,
            [
                new(
                    "Volgorde in de paneelflow",
                    "De editor werkt het best als u eerst de wand vastzet en daarna selecteren, plaatsen, controleren en opslaan uit elkaar houdt.",
                    [
                        "Open eerst één wand zodat de visualisatie en paneel-editor dezelfde context gebruiken.",
                        "Gebruik hints voor standaardwaarden zoals randspeling.",
                        "Open info-knoppen bij scharnierzijde en pot-hart als een term technisch voelt.",
                        "Gebruik details om de maatberekening of de leesrichting van de tekening na te lopen."
                    ]),
                new(
                    "Wanneer heeft u diepere hulp nodig?",
                    "De zijhulp is vooral nuttig als de opening niet vanzelfsprekend is of als meerdere kasten en apparaten binnen dezelfde wand elkaar raken.",
                    [
                        "Open de verdiepende hulp bij complexe combinaties met apparaten, vrije segmenten of afwijkende scharnierkeuzes.",
                        "Laat niet-actieve wanden verder compact zodat plaatsen en opslaan de hoofdtaak blijven."
                    ])
            ]),
        ["verificatie"] = new(
            "verificatie",
            "Hulp bij stap 3: Verificatie",
            "Verificatie draait om een korte checklist: eerst de maat in de opening, daarna pas de technische onderbouwing.",
            StandaardHulpNiveaus,
            [
                new(
                    "Checklist eerst, techniek daarna",
                    "Gebruik de hoofdschermen om af te vinken wat u echt na moet meten; technische verdieping hoeft niet standaard open te staan.",
                    [
                        "Meet eerst de opening na en vergelijk daarna de maat om te bestellen.",
                        "Open details alleen als u de afleiding of scharnieronderbouwing wilt nalopen."
                    ]),
                new(
                    "Waar helpt de drawer bij?",
                    "De verdiepende hulp zet de controlevolgorde en voorbeelden per paneeltype bij elkaar.",
                    [
                        "Gebruik deze hulp als u wilt weten wanneer een deur ook een scharniercontrole nodig heeft.",
                        "De drawer is bedoeld voor taakgerichte uitleg, niet voor elke losse maat."
                    ])
            ]),
        ["bestellijst"] = new(
            "bestellijst",
            "Hulp bij stap 4: Bestellijst",
            "De bestellijst focust standaard op orderregels. Technische exportinformatie hoort beschikbaar te zijn zonder de hoofdweergave over te nemen.",
            StandaardHulpNiveaus,
            [
                new(
                    "Orderregels versus techniek",
                    "Gebruik de basisweergave om aantallen, typen en wanden te controleren. Open technische informatie alleen voor overdracht of nabewerking.",
                    [
                        "De hint onder de technische toggle legt uit wanneer u CNC- en boorgatdetails nodig hebt.",
                        "Het detailsblok bundelt exportinformatie zodat die niet permanent boven de tabel hangt."
                    ]),
                new(
                    "Diepere exporthulp",
                    "De zijhulp helpt vooral bij de keuze tussen PDF en Excel en bij het beslissen of technische details nodig zijn.",
                    [
                        "Gebruik de verdiepende hulp als iemand de output deelt met een machinebewerker of werkvoorbereider.",
                        "Laat de ordertabel verder de primaire interface blijven."
                    ])
            ]),
        ["zaagplan"] = new(
            "zaagplan",
            "Hulp bij stap 5: Zaagplan",
            "In het zaagplan zijn vooral plaatmaat, zaagverlies en draaien bepalend. Die termen verdienen korte uitleg dicht bij het veld en langere uitleg op aanvraag.",
            StandaardHulpNiveaus,
            [
                new(
                    "Instellingen compact houden",
                    "Gebruik hints voor standaardwaarden en info-knoppen voor begrippen die invloed hebben op de berekening.",
                    [
                        "Zaagverlies per snede en draaien toestaan zijn goede kandidaten voor korte uitleg direct naast het veld.",
                        "Open de drawer alleen als u de gevolgen voor opbrengst of plaatsbaarheid wilt doorgronden."
                    ]),
                new(
                    "Resultaat lezen",
                    "De hoofdtaak blijft het beoordelen van de plaatindeling; diepere hulp mag dat niet blokkeren.",
                    [
                        "Gebruik de verdiepende hulp als panelen niet passen of als u de instellingen wilt vergelijken.",
                        "Laat de visuele plaatindeling het dominante deel van de pagina."
                    ])
            ])
    };

    public static IReadOnlyList<HulpNiveau> HulpNiveaus => StandaardHulpNiveaus;

    public static StapHulpContext VoorStap(string stapId)
        => StapHulp.TryGetValue(stapId, out var context)
            ? context
            : throw new ArgumentOutOfRangeException(nameof(stapId), stapId, "Onbekende staphulp.");
}
