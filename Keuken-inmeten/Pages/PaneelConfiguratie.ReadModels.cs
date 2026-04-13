namespace Keuken_inmeten.Pages;

using System.Globalization;
using Keuken_inmeten.Components;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

public partial class PaneelConfiguratie
{
    private PaneelConfiguratiePaginaModel HuidigePaginaModel
    {
        get
        {
            var paneelEditorStatus = HuidigePaneelEditorStatus;

            return PaneelConfiguratieReadModelHelper.BouwPaginaModel(
                routeGate: StappenFlowHelper.BepaalRouteGate("panelen", StappenFlowHelper.BepaalStatus(State)),
                wandWerkruimtes: State.LeesPaneelWerkruimtes(bewerkToewijzingId),
                paneelReviewGroepen: OverzichtGroeperingHelper.GroepeerPaneelToewijzingen(State),
                paneelTypeTellingen: OverzichtGroeperingHelper.MaakTellingen(State.Toewijzingen, toewijzing => TypeNaam(toewijzing.Type)),
                toewijzingenAantal: State.Toewijzingen.Count,
                geopendeWandId: geopendeWandId,
                isReviewWeergaveActief: IsReviewWeergaveActief,
                toonEditorDrawer: toonEditorDrawer,
                paneelRandSpeling: State.PaneelRandSpeling,
                paneelEditorStatus: paneelEditorStatus);
        }
    }
}

public static class PaneelConfiguratieReadModelHelper
{
    public static PaneelConfiguratiePaginaModel BouwPaginaModel(
        StapRouteGate? routeGate,
        IReadOnlyList<PaneelWerkruimteContext> wandWerkruimtes,
        IReadOnlyList<OverzichtGroeperingHelper.PaneelReviewGroep> paneelReviewGroepen,
        IReadOnlyList<OverzichtGroeperingHelper.Telling> paneelTypeTellingen,
        int toewijzingenAantal,
        Guid? geopendeWandId,
        bool isReviewWeergaveActief,
        bool toonEditorDrawer,
        double paneelRandSpeling,
        PaneelEditorStatusModel paneelEditorStatus)
    {
        var reviewBeschikbaar = toewijzingenAantal > 0;
        var wandSamenvattingen = wandWerkruimtes
            .Select(werkruimte => BouwWandSamenvatting(werkruimte, geopendeWandId))
            .ToList();

        var actieveWandSamenvatting = geopendeWandId is Guid wandId
            ? wandSamenvattingen.FirstOrDefault(item => item.Werkruimte.Wand.Id == wandId)
            : null;
        var overzichtWanden = actieveWandSamenvatting is null
            ? wandSamenvattingen
            : wandSamenvattingen
                .Where(item => item.Werkruimte.Wand.Id != actieveWandSamenvatting.Werkruimte.Wand.Id)
                .ToList();
        var actieveWerkruimte = actieveWandSamenvatting is null
            ? null
            : BouwActieveWerkruimteModel(actieveWandSamenvatting, toonEditorDrawer, paneelEditorStatus);
        var toonCompacteStapIntro = routeGate is null && actieveWerkruimte is not null && !isReviewWeergaveActief;
        var toonCompacteWeergaveTabs = toonEditorDrawer && !isReviewWeergaveActief && actieveWerkruimte is not null;

        return new(
            RouteGate: routeGate,
            ToewijzingenAantal: toewijzingenAantal,
            IsReviewWeergaveActief: isReviewWeergaveActief,
            ToonEditorDrawer: toonEditorDrawer,
            ReviewBeschikbaar: reviewBeschikbaar,
            ToonCompacteStapIntro: toonCompacteStapIntro,
            ToonCompacteWeergaveTabs: toonCompacteWeergaveTabs,
            ToonUitgeklapteProjectinstellingen: Math.Abs(paneelRandSpeling - 3) > 0.001,
            Kop: BouwKopModel(routeGate, isReviewWeergaveActief, actieveWerkruimte),
            WeergaveTabs: BouwWeergaveTabsModel(
                actieveWerkruimte,
                isReviewWeergaveActief,
                reviewBeschikbaar,
                toewijzingenAantal,
                toonEditorDrawer,
                toonCompacteWeergaveTabs),
            PaneelEditorStatus: paneelEditorStatus,
            OverzichtWanden: overzichtWanden,
            ActieveWerkruimte: actieveWerkruimte,
            ReviewGroepen: RangschikPaneelReviewGroepen(paneelReviewGroepen, actieveWerkruimte?.Samenvatting.Werkruimte.Wand.Id),
            PaneelTypeTellingen: paneelTypeTellingen);
    }

    private static PaneelPaginaKopModel BouwKopModel(
        StapRouteGate? routeGate,
        bool isReviewWeergaveActief,
        PaneelActieveWerkruimteModel? actieveWerkruimte)
    {
        if (routeGate is null && isReviewWeergaveActief)
        {
            return new(
                LeadTekst: "Loop aantallen, maten en acties per wand rustig na. Ga alleen terug naar de editor als u opnieuw wilt plaatsen of bewerken.",
                CompactBlok: null);
        }

        if (routeGate is null && actieveWerkruimte is not null)
        {
            return new(
                LeadTekst: null,
                CompactBlok: new(
                    Kicker: "Actieve wand",
                    Titel: actieveWerkruimte.Samenvatting.Werkruimte.Wand.Naam,
                    Beschrijving: "Rond selectie, maat en opslaan af zonder extra schermwissels."));
        }

        return new(
            LeadTekst: "Open één wand en plaats daarna panelen in de editor. Het overzicht wordt pas nuttig zodra er panelen zijn.",
            CompactBlok: null);
    }

    private static PaneelWeergaveTabsModel BouwWeergaveTabsModel(
        PaneelActieveWerkruimteModel? actieveWerkruimte,
        bool isReviewWeergaveActief,
        bool reviewBeschikbaar,
        int toewijzingenAantal,
        bool toonEditorDrawer,
        bool toonCompacteWeergaveTabs)
    {
        if (toonCompacteWeergaveTabs)
        {
            return new(
                IsCompact: true,
                Titel: $"Editor open voor {actieveWerkruimte?.Samenvatting.Werkruimte.Wand.Naam ?? "Paneel-editor"}",
                Beschrijving: "Rond selectie, maat en opslaan af zonder extra schermwissels.",
                MetaItems: reviewBeschikbaar
                    ? [$"{toewijzingenAantal} paneel/panelen klaar"]
                    : []);
        }

        var metaItems = new List<string>();
        if (actieveWerkruimte is not null && !isReviewWeergaveActief)
        {
            metaItems.Add($"Actief: {actieveWerkruimte.Samenvatting.Werkruimte.Wand.Naam}");
            metaItems.Add(toonEditorDrawer ? "Editor open" : "Editor gesloten");
        }

        if (reviewBeschikbaar)
            metaItems.Add($"{toewijzingenAantal} paneel/panelen");

        var titel = isReviewWeergaveActief
            ? "Overzicht van toegewezen panelen"
            : actieveWerkruimte is null
                ? "Open eerst één wand"
                : $"Editor — {actieveWerkruimte.Samenvatting.Werkruimte.Wand.Naam}";
        string? beschrijving = isReviewWeergaveActief
            ? "Controleer hier aantallen, maten en acties zonder het canvas erboven."
            : actieveWerkruimte is not null
                ? toonEditorDrawer
                    ? "De editor staat open. Rond selectie, maat en opslaan in één werklaag af."
                    : "Werk op één wand tegelijk en open de editor zodra uw selectie klaarstaat."
                : null;

        return new(
            IsCompact: false,
            Titel: titel,
            Beschrijving: beschrijving,
            MetaItems: metaItems);
    }

    private static PaneelActieveWerkruimteModel BouwActieveWerkruimteModel(
        PaneelWandSamenvatting samenvatting,
        bool toonEditorDrawer,
        PaneelEditorStatusModel paneelEditorStatus)
    {
        var metaItems = new List<string>
        {
            $"{samenvatting.Werkruimte.Kasten.Count} kast(en)",
            $"{samenvatting.Werkruimte.Toewijzingen.Count} paneel/panelen"
        };

        if (samenvatting.Werkruimte.Apparaten.Count > 0)
            metaItems.Add($"{samenvatting.Werkruimte.Apparaten.Count} apparaat(en)");

        metaItems.Add(toonEditorDrawer ? "Editor open" : "Editor gesloten");

        var werkruimteStatus = toonEditorDrawer
            ? null
            : new PaneelWerkruimteStatusSamenvatting(
                Kicker: "Volgende stap",
                Titel: paneelEditorStatus.VolgendePaneelStapTekst,
                Beschrijving: paneelEditorStatus.WerkruimteStatusDetailTekst);

        return new(samenvatting, metaItems, werkruimteStatus);
    }

    private static PaneelWandSamenvatting BouwWandSamenvatting(
        PaneelWerkruimteContext werkruimte,
        Guid? geopendeWandId)
    {
        var wand = werkruimte.Wand;
        var aantalKasten = werkruimte.Kasten.Count;
        var aantalPanelen = werkruimte.Toewijzingen.Count;
        var aantalApparaten = werkruimte.Apparaten.Count;
        var isActief = geopendeWandId == wand.Id;

        IReadOnlyList<IndelingBadge> overzichtBadges = isActief
            ? [new IndelingBadge("Nu open", "badge text-bg-primary")]
            : [];
        var overzichtMetaItems = new List<string>();
        if (aantalKasten > 0)
            overzichtMetaItems.Add($"{aantalKasten} kast(en)");
        if (aantalPanelen > 0)
            overzichtMetaItems.Add($"{aantalPanelen} paneel/panelen");
        overzichtMetaItems.Add($"{FormatMaat(wand.Breedte)} mm wand");

        var schakelaarMetaItems = new List<string> { $"{aantalKasten} kast(en)" };
        if (aantalPanelen > 0)
            schakelaarMetaItems.Add($"{aantalPanelen} paneel/panelen");
        schakelaarMetaItems.Add($"{FormatMaat(wand.Breedte)} mm wand");

        return new(
            Werkruimte: werkruimte,
            IsActief: isActief,
            OverzichtBadges: overzichtBadges,
            OverzichtMetaItems: overzichtMetaItems,
            OverzichtBeschrijving: aantalKasten == 0
                ? "Voeg eerst een kast toe in stap 1."
                : aantalPanelen > 0
                    ? "Open deze wand om panelen te bekijken of te bewerken."
                    : "Open deze wand om uw eerste paneel te plaatsen.",
            OverzichtKnopLabel: aantalKasten == 0
                ? "Eerst stap 1"
                : isActief
                    ? "Werkruimte geopend"
                    : "Open wand",
            OverzichtKnopClass: aantalKasten == 0 ? "btn btn-outline-secondary" : "btn btn-primary",
            OverzichtKnopUitgeschakeld: isActief || aantalKasten == 0,
            SchakelaarMetaItems: schakelaarMetaItems,
            SchakelaarBeschrijving: aantalKasten == 0
                ? "Nog geen kasten op deze wand. Voeg die eerst in stap 1 toe."
                : $"{aantalPanelen} paneel/panelen · {aantalApparaten} apparaat(en) · {FormatMaat(wand.Breedte)} mm wandbreedte",
            SchakelaarKnopLabel: aantalKasten == 0 ? "Eerst kast toevoegen" : "Open wand",
            SchakelaarKnopUitgeschakeld: aantalKasten == 0);
    }

    private static IReadOnlyList<OverzichtGroeperingHelper.PaneelReviewGroep> RangschikPaneelReviewGroepen(
        IReadOnlyList<OverzichtGroeperingHelper.PaneelReviewGroep> paneelReviewGroepen,
        Guid? geopendeWandId)
        => geopendeWandId is null
            ? paneelReviewGroepen
            : paneelReviewGroepen
                .OrderBy(groep => groep.WandId == geopendeWandId ? 0 : 1)
                .ThenBy(groep => groep.WandNaam, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(groep => groep.WandId?.ToString("N"), StringComparer.Ordinal)
                .ToList();

    private static string FormatMaat(double value)
        => value.ToString("0.#", CultureInfo.CurrentCulture);
}

public sealed record PaneelConfiguratiePaginaModel(
    StapRouteGate? RouteGate,
    int ToewijzingenAantal,
    bool IsReviewWeergaveActief,
    bool ToonEditorDrawer,
    bool ReviewBeschikbaar,
    bool ToonCompacteStapIntro,
    bool ToonCompacteWeergaveTabs,
    bool ToonUitgeklapteProjectinstellingen,
    PaneelPaginaKopModel Kop,
    PaneelWeergaveTabsModel WeergaveTabs,
    PaneelEditorStatusModel PaneelEditorStatus,
    IReadOnlyList<PaneelWandSamenvatting> OverzichtWanden,
    PaneelActieveWerkruimteModel? ActieveWerkruimte,
    IReadOnlyList<OverzichtGroeperingHelper.PaneelReviewGroep> ReviewGroepen,
    IReadOnlyList<OverzichtGroeperingHelper.Telling> PaneelTypeTellingen);

public sealed record PaneelPaginaKopModel(
    string? LeadTekst,
    PaneelCompacteKopModel? CompactBlok);

public sealed record PaneelCompacteKopModel(
    string Kicker,
    string Titel,
    string Beschrijving);

public sealed record PaneelWeergaveTabsModel(
    bool IsCompact,
    string Titel,
    string? Beschrijving,
    IReadOnlyList<string> MetaItems);

public sealed record PaneelWandSamenvatting(
    PaneelWerkruimteContext Werkruimte,
    bool IsActief,
    IReadOnlyList<IndelingBadge> OverzichtBadges,
    IReadOnlyList<string> OverzichtMetaItems,
    string OverzichtBeschrijving,
    string OverzichtKnopLabel,
    string OverzichtKnopClass,
    bool OverzichtKnopUitgeschakeld,
    IReadOnlyList<string> SchakelaarMetaItems,
    string SchakelaarBeschrijving,
    string SchakelaarKnopLabel,
    bool SchakelaarKnopUitgeschakeld);

public sealed record PaneelActieveWerkruimteModel(
    PaneelWandSamenvatting Samenvatting,
    IReadOnlyList<string> MetaItems,
    PaneelWerkruimteStatusSamenvatting? WerkruimteStatus);

public sealed record PaneelWerkruimteStatusSamenvatting(
    string Kicker,
    string Titel,
    string Beschrijving);
