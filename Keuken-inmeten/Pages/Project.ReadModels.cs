namespace Keuken_inmeten.Pages;

public static class ProjectReadModelHelper
{
    public static ProjectPaginaModel BouwPaginaModel(
        int aantalWanden,
        int aantalKasten,
        int aantalPanelen,
        int aantalBestellijstItems,
        int totaalBoorgaten,
        bool heeftProjectInhoud,
        double paneelRandSpeling,
        bool isDarkTheme)
    {
        var heeftKeukenData = aantalWanden > 0 || aantalKasten > 0 || aantalPanelen > 0;
        var metaItems = new List<string>
        {
            $"{aantalWanden} wand(en)",
            $"{aantalKasten} kast(en)",
            $"{aantalPanelen} paneel/panelen"
        };

        if (aantalBestellijstItems > 0)
        {
            metaItems.Add($"{aantalBestellijstItems} orderregel(s)");
        }
        else if (totaalBoorgaten > 0)
        {
            metaItems.Add(FormatPotscharnierGatTelling(totaalBoorgaten));
        }
        else if (!heeftProjectInhoud)
        {
            metaItems.Add("Nog geen opgeslagen projectinhoud");
        }

        return new(
            BadgeTekst: heeftKeukenData ? "Project in uitvoering" : "Projectopties",
            LeadTekst: heeftKeukenData
                ? "Beheer projectbrede instellingen en deel of bewaar de huidige keuken vanaf een centrale plek."
                : "Pas projectbrede instellingen aan, laad een eerder project of begin daarna met wanden en kasten.",
            StatusTitel: heeftKeukenData ? "Huidige stand van dit project" : "Nog geen keukeninhoud geladen",
            StatusBeschrijving: MaakStatusBeschrijving(
                heeftKeukenData,
                aantalWanden,
                aantalKasten,
                aantalPanelen,
                aantalBestellijstItems,
                totaalBoorgaten),
            MetaItems: metaItems,
            RandSpelingSamenvatting: $"Nu {paneelRandSpeling:0.#} mm totaal. Open dit alleen als het hele project bewust meer of minder voeg nodig heeft.",
            UiterlijkBeschrijving: isDarkTheme
                ? "Donker uiterlijk is actief. Wissel terug als u liever in een lichtere werkruimte meet."
                : "Licht uiterlijk is actief. Schakel over als u liever in een donkerder werkruimte meet.",
            KanWissen: heeftProjectInhoud);
    }

    private static string MaakStatusBeschrijving(
        bool heeftKeukenData,
        int aantalWanden,
        int aantalKasten,
        int aantalPanelen,
        int aantalBestellijstItems,
        int totaalBoorgaten)
    {
        if (!heeftKeukenData)
            return "Voeg wanden en kasten toe of importeer een eerder opgeslagen project om verder te gaan.";

        if (aantalBestellijstItems > 0)
            return $"{aantalBestellijstItems} orderregel(s) en {FormatPotscharnierGatTelling(totaalBoorgaten)} staan klaar voor controle, delen of export.";

        if (aantalPanelen > 0)
            return $"{aantalPanelen} paneel/panelen zijn toegewezen. Rond nu verificatie af zodra maat en scharnierdata kloppen.";

        return $"{aantalWanden} wand(en) en {aantalKasten} kast(en) staan klaar. Wijs daarna panelen toe om bestellijst en zaagplan op te bouwen.";
    }

    private static string FormatPotscharnierGatTelling(int aantal)
        => $"{aantal} {(aantal == 1 ? "35 mm potscharniergat" : "35 mm potscharniergaten")}";
}

public sealed record ProjectPaginaModel(
    string BadgeTekst,
    string LeadTekst,
    string StatusTitel,
    string StatusBeschrijving,
    IReadOnlyList<string> MetaItems,
    string RandSpelingSamenvatting,
    string UiterlijkBeschrijving,
    bool KanWissen);
