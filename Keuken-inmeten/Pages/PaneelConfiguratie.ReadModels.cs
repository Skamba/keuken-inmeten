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
                toewijzingenAantal: State.Toewijzingen.Count,
                geopendeWandId: geopendeWandId,
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
        int toewijzingenAantal,
        Guid? geopendeWandId,
        bool toonEditorDrawer,
        double paneelRandSpeling,
        PaneelEditorStatusModel paneelEditorStatus)
    {
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
        var actieveReviewGroep = actieveWerkruimte is null
            ? null
            : paneelReviewGroepen.FirstOrDefault(groep => groep.WandId == actieveWerkruimte.Samenvatting.Werkruimte.Wand.Id);
        var toonCompacteStapIntro = routeGate is null && actieveWerkruimte is not null;

        return new(
            RouteGate: routeGate,
            ToewijzingenAantal: toewijzingenAantal,
            ToonEditorDrawer: toonEditorDrawer,
            ToonCompacteStapIntro: toonCompacteStapIntro,
            ToonUitgeklapteProjectinstellingen: Math.Abs(paneelRandSpeling - 3) > 0.001,
            Kop: BouwKopModel(routeGate, actieveWerkruimte),
            PaneelEditorStatus: paneelEditorStatus,
            OverzichtWanden: overzichtWanden,
            ActieveWerkruimte: actieveWerkruimte,
            ActieveReviewGroep: actieveReviewGroep);
    }

    private static PaneelPaginaKopModel BouwKopModel(
        StapRouteGate? routeGate,
        PaneelActieveWerkruimteModel? actieveWerkruimte)
    {
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
            LeadTekst: "Open één wand en plaats of controleer daarna panelen in één werklaag.",
            CompactBlok: null);
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

    private static string FormatMaat(double value)
        => value.ToString("0.#", CultureInfo.CurrentCulture);
}

public sealed record PaneelConfiguratiePaginaModel(
    StapRouteGate? RouteGate,
    int ToewijzingenAantal,
    bool ToonEditorDrawer,
    bool ToonCompacteStapIntro,
    bool ToonUitgeklapteProjectinstellingen,
    PaneelPaginaKopModel Kop,
    PaneelEditorStatusModel PaneelEditorStatus,
    IReadOnlyList<PaneelWandSamenvatting> OverzichtWanden,
    PaneelActieveWerkruimteModel? ActieveWerkruimte,
    OverzichtGroeperingHelper.PaneelReviewGroep? ActieveReviewGroep);

public sealed record PaneelPaginaKopModel(
    string? LeadTekst,
    PaneelCompacteKopModel? CompactBlok);

public sealed record PaneelCompacteKopModel(
    string Kicker,
    string Titel,
    string Beschrijving);

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
