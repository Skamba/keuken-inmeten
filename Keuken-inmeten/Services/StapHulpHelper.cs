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
            "Bijvoorbeeld: standaard 3 mm totale voeg of meestal 18 mm wanddikte."),
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
            "Gebruik de zijhulp voor langere stapgerichte uitleg, voorbeelden en beslisregels terwijl het stappenverloop intact blijft.",
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
                        "Gebruik de korte hints voor naam, wand en maatverwachtingen; die voorbeelden blijven zichtbaar terwijl u typt.",
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
            "In deze stap opent u eerst precies één wand als werkcontext, kiest daarna de juiste kast(en) en past pas daarna maat, richting en technische details van het paneel aan. Het overzicht van toegewezen panelen opent u pas apart wanneer u lijst, status en acties wilt nalopen.",
            StandaardHulpNiveaus,
            [
                new(
                    "Volgorde in de paneelstap",
                    "De editor werkt het best als u eerst de wand vastzet en daarna selecteren, plaatsen, controleren en opslaan uit elkaar houdt. Het overzicht hoort pas daarna in een aparte weergave.",
                    [
                        "Open eerst één wand zodat de visualisatie en paneel-editor dezelfde context gebruiken.",
                        "Schakel pas naar Toegewezen panelen als u lijst, status en acties wilt nalopen zonder het canvas erbij.",
                        "Gebruik hints voor standaardwaarden zoals randspeling.",
                        "Open info-knoppen bij scharnierzijde en pot-hart als een term technisch voelt.",
                        "Gebruik details om de maatberekening of de leesrichting van de tekening na te lopen."
                    ]),
                new(
                    "Wanneer heeft u diepere hulp nodig?",
                    "De zijhulp is vooral nuttig als de opening niet vanzelfsprekend is of als meerdere kasten en apparaten binnen dezelfde wand elkaar raken.",
                    [
                        "Open de verdiepende hulp bij complexe combinaties met apparaten, vrije segmenten of afwijkende scharnierkeuzes.",
                        "Laat niet-actieve wanden verder compact en gebruik het overzicht apart, zodat plaatsen en opslaan de hoofdtaak blijven."
                    ])
            ]),
        ["verificatie"] = new(
            "verificatie",
            "Hulp bij stap 3: Verificatie",
            "Verificatie draait om een taaklijst per wand en paneel: open alleen de controle die u nu echt wilt nalopen.",
            StandaardHulpNiveaus,
            [
                new(
                    "Taaklijst eerst, techniek daarna",
                    "Gebruik de taaklijst om per wand en paneel te kiezen wat u nu controleert; technische verdieping hoeft niet standaard open te staan.",
                    [
                        "Open per paneel de taak en meet eerst de opening na; vergelijk daarna pas de maat om te bestellen.",
                        "Open details alleen als u de afleiding of scharnieronderbouwing wilt nalopen."
                    ]),
                new(
                    "Waar helpt de verdieping bij?",
                    "De verdiepende hulp zet de controlevolgorde en voorbeelden per paneeltype bij elkaar zodra u een taak opent.",
                    [
                        "Gebruik deze hulp als u wilt weten wanneer een deur ook een scharniercontrole nodig heeft.",
                        "De verdieping is bedoeld voor taakgerichte uitleg, niet voor elke losse maat."
                    ])
            ]),
        ["bestellijst"] = new(
            "bestellijst",
            "Hulp bij stap 4: Bestellijst",
            "De bestellijst richt zich standaard op orderregels. Export kiest u nu via aparte stappen met voorvertoning en bevestiging, zodat de controletabel rustig blijft.",
            StandaardHulpNiveaus,
            [
                new(
                    "Orderregels versus techniek",
                    "Gebruik de basisweergave om aantallen, typen en wanden te controleren. Start export pas als de ordercontrole klopt; technische details en formaatkeuze staan los van de exportstappen.",
                    [
                        "De schakelaar voor technische details is alleen bedoeld voor controle of overdracht van de tabel.",
                        "Materiaal, dikte en formaat verschijnen pas wanneer u de aparte exportstappen opent.",
                        "Voorbeeldwaarden bij paneeltype en dikte blijven zichtbaar naast het veld, ook als u al begint te typen."
                    ]),
                new(
                    "Exportstappen met voorvertoning",
                    "De zijhulp helpt vooral bij de keuze tussen PDF en Excel en laat eerst zien wat u straks downloadt of opent.",
                    [
                        "Gebruik de voorvertoning om te controleren of u een rustig PDF-document of juist een bewerkbare Excel-lijst nodig hebt.",
                        "Bevestig pas in de laatste stap; zo blijft de bestellijstpagina zelf een rustige controleweergave."
                    ])
            ]),
        ["zaagplan"] = new(
            "zaagplan",
            "Hulp bij stap 5: Zaagplan",
            "In het zaagplan blijft de plaatindeling nu dominant. Plaatmaat en weergave staan compact bovenaan; zaagverlies en draaien opent u alleen als geavanceerde instellingen.",
            StandaardHulpNiveaus,
            [
                new(
                    "Instellingen compact houden",
                    "Houd plaatafmetingen en de weergavekeuze direct in beeld. Open geavanceerde instellingen alleen voor zaagverlies of draaien.",
                    [
                        "Plaatbreedte en plaathoogte blijven de primaire instellingen voor een snelle eerste check.",
                        "Zaagverlies per snede en draaien toestaan mogen standaard dicht blijven achter `Geavanceerde instellingen`."
                    ]),
                new(
                    "Resultaat lezen",
                    "De hoofdtaak blijft het beoordelen van de plaatindeling; wissel zo nodig tussen alle platen en één plaat tegelijk.",
                    [
                        "Gebruik `Één plaat tegelijk` als u een specifieke plaat rustig wilt nalopen.",
                        "Schakel terug naar `Alle platen` als u opbrengst of verdeling tussen platen wilt vergelijken."
                    ])
            ])
    };

    public static IReadOnlyList<HulpNiveau> HulpNiveaus => StandaardHulpNiveaus;

    public static StapHulpContext VoorStap(string stapId)
        => StapHulp.TryGetValue(stapId, out var context)
            ? context
            : throw new ArgumentOutOfRangeException(nameof(stapId), stapId, "Onbekende staphulp.");
}
