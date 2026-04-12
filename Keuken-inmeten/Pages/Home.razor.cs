using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class Home
{
    private HomePaginaModel MaakPaginaModel()
    {
        var aantalWanden = State.Wanden.Count;
        var aantalKasten = State.Kasten.Count;
        var aantalPanelen = State.Toewijzingen.Count;
        var aantalResultaten = State.BerekenResultaten().Count;
        var bestellijstItems = BestellijstService.BerekenItems(State);
        var aantalBestellijstItems = bestellijstItems.Count;
        var totaalBoorgaten = bestellijstItems.Sum(item => item.Aantal * item.Boorgaten.Count);
        var heeftProjectData = aantalWanden > 0 || aantalKasten > 0 || aantalPanelen > 0;
        var flowStatus = StappenFlowHelper.BepaalStatus(State);

        return new HomePaginaModel(
            AantalWanden: aantalWanden,
            AantalKasten: aantalKasten,
            AantalPanelen: aantalPanelen,
            AantalResultaten: aantalResultaten,
            AantalBestellijstItems: aantalBestellijstItems,
            TotaalBoorgaten: totaalBoorgaten,
            HeeftProjectData: heeftProjectData,
            VervolgStap: StappenFlowHelper.BepaalVervolgStap(flowStatus),
            Stappen: BouwStappen(flowStatus, aantalWanden, aantalKasten, aantalPanelen, aantalResultaten, aantalBestellijstItems),
            DashboardLead: MaakDashboardLead(aantalWanden, aantalKasten, aantalPanelen, aantalBestellijstItems),
            AandachtBlok: MaakAandachtBlok(aantalPanelen, aantalBestellijstItems),
            BoorgatBlok: MaakBoorgatBlok(totaalBoorgaten),
            OutputSamenvatting: MaakOutputSamenvatting(aantalResultaten, aantalBestellijstItems, totaalBoorgaten));
    }

    private static StartStap[] BouwStappen(StappenFlowStatus flowStatus, int aantalWanden, int aantalKasten, int aantalPanelen, int aantalResultaten, int aantalBestellijstItems)
    {
        var aanbevolenStap = StappenFlowHelper.BepaalVervolgStap(flowStatus);

        return StappenFlowHelper.AlleStappen
            .Select(stap =>
            {
                var heeftData = stap.Id switch
                {
                    "kasten" => aantalWanden > 0 || aantalKasten > 0,
                    "panelen" => aantalPanelen > 0,
                    "verificatie" => aantalResultaten > 0,
                    "bestellijst" => aantalBestellijstItems > 0,
                    "zaagplan" => aantalBestellijstItems > 0,
                    _ => false
                };

                var samenvatting = stap.Id switch
                {
                    "kasten" => aantalWanden > 0 ? $"{aantalWanden} wand(en) · {aantalKasten} kast(en)" : "Start met wanden en kasten",
                    "panelen" => aantalPanelen > 0 ? $"{aantalPanelen} paneel/panelen toegewezen" : "Nog geen panelen toegewezen",
                    "verificatie" => aantalResultaten > 0 ? $"{aantalResultaten} paneel/panelen klaar om te controleren" : "Beschikbaar zodra panelen zijn geplaatst",
                    "bestellijst" => aantalBestellijstItems > 0 ? $"{aantalBestellijstItems} orderregel(s) beschikbaar" : "Verschijnt zodra er panelen klaarstaan",
                    "zaagplan" => aantalBestellijstItems > 0 ? "Gebruik de orderregels om platen slim te vullen" : "Bouwt voort op de panelen uit uw bestellijst",
                    _ => string.Empty
                };

                return MaakStap(
                    stap,
                    samenvatting,
                    heeftData,
                    stap.Id == aanbevolenStap.Id,
                    StappenFlowHelper.BepaalRouteGate(stap.Id, flowStatus));
            })
            .ToArray();
    }

    private static StartStap MaakStap(
        AppStap stap,
        string samenvatting,
        bool heeftData,
        bool isAanbevolen,
        StapRouteGate? routeGate)
    {
        var badgeLabel = isAanbevolen
            ? "Volgende stap"
            : routeGate is not null
                ? $"Eerst {routeGate.VereisteStap.Label}"
                : heeftData
                    ? "Beschikbaar"
                    : "Klaar om te starten";
        var badgeClass = isAanbevolen
            ? "bg-primary-subtle text-primary-emphasis border border-primary-subtle"
            : routeGate is not null
                ? "bg-body-tertiary text-body-secondary border"
                : heeftData
                    ? "bg-success-subtle text-success-emphasis border border-success-subtle"
                    : "bg-body-tertiary text-body-secondary border";

        return new StartStap(
            routeGate?.VereisteStap.Route ?? stap.Route,
            stap.Nummer,
            stap.Label,
            stap.Beschrijving,
            routeGate?.Reden ?? samenvatting,
            badgeLabel,
            badgeClass,
            routeGate is null);
    }

    private static string MaakDashboardLead(int aantalWanden, int aantalKasten, int aantalPanelen, int aantalBestellijstItems)
        => aantalBestellijstItems > 0
            ? $"{aantalWanden} wand(en), {aantalKasten} kast(en) en {aantalPanelen} paneel/panelen staan klaar. Hervat nu bij de volgende controle- of uitvoerstap."
            : aantalPanelen > 0
                ? "Uw indeling en paneeltoewijzing staan klaar. Rond nu verificatie af en werk daarna door naar bestellijst en zaagplan."
                : $"{aantalWanden} wand(en) en {aantalKasten} kast(en) staan klaar. Ga nu verder met panelen toewijzen.";

    private static InfoBlok MaakAandachtBlok(int aantalPanelen, int aantalBestellijstItems)
        => aantalPanelen == 0
            ? new(
                "Wijs eerst panelen toe",
                "Zonder paneeltoewijzing blijven verificatie, bestellijst en zaagplan gesloten.")
            : aantalBestellijstItems == 0
                ? new(
                    "Rond verificatie af",
                    "Controleer eerst maat, scharnierzijde en 35 mm potscharniergaten voordat u bestelt.")
                : new(
                    "Bestellijst en zaagplan zijn klaar",
                    "U kunt nu exporteren, orderregels nalopen en het plaatformaat controleren.");

    private static InfoBlok MaakBoorgatBlok(int totaalBoorgaten)
        => totaalBoorgaten > 0
            ? new(
                $"{FormatPotscharnierGatTelling(totaalBoorgaten)} al berekend",
                "U vindt deze terug in verificatie en bestellijst.")
            : new(
                "35 mm potscharniergaten verschijnen later",
                "Zodra deuren of relevante fronten zijn toegewezen vult de app deze automatisch aan.");

    private static string MaakOutputSamenvatting(int aantalResultaten, int aantalBestellijstItems, int totaalBoorgaten)
        => aantalBestellijstItems > 0
            ? $"{aantalBestellijstItems} orderregel(s) en {FormatPotscharnierGatTelling(totaalBoorgaten)} klaar voor uitvoer"
            : aantalResultaten > 0
                ? $"{aantalResultaten} paneel/panelen klaar voor controle"
                : "Nog geen uitvoer; wijs eerst panelen toe";

    private static string FormatPotscharnierGatTelling(int aantal)
        => $"{aantal} {(aantal == 1 ? "35 mm potscharniergat" : "35 mm potscharniergaten")}";

    private sealed record HomePaginaModel(
        int AantalWanden,
        int AantalKasten,
        int AantalPanelen,
        int AantalResultaten,
        int AantalBestellijstItems,
        int TotaalBoorgaten,
        bool HeeftProjectData,
        AppStap VervolgStap,
        StartStap[] Stappen,
        string DashboardLead,
        InfoBlok AandachtBlok,
        InfoBlok BoorgatBlok,
        string OutputSamenvatting);

    private sealed record StartStap(
        string Href,
        int Nummer,
        string Titel,
        string Beschrijving,
        string Samenvatting,
        string BadgeLabel,
        string BadgeClass,
        bool IsBeschikbaar);

    private sealed record InfoBlok(string Titel, string Tekst);
}
