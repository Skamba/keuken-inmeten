using Keuken_inmeten.Models;
using Keuken_inmeten.Pages;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PaneelConfiguratieReadModelHelperTests
{
    [Fact]
    public void BouwPaginaModel_zet_actieve_wand_centraal_en_reviewgroep_vooraan()
    {
        var wandA = MaakWerkruimte("Linkerwand", aantalKasten: 1, aantalPanelen: 0);
        var wandB = MaakWerkruimte("Rechterwand", aantalKasten: 2, aantalPanelen: 1);

        var model = PaneelConfiguratieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wandWerkruimtes: [wandA, wandB],
            paneelReviewGroepen:
            [
                MaakReviewGroep(wandA.Wand, aantalPanelen: 1),
                MaakReviewGroep(wandB.Wand, aantalPanelen: 2)
            ],
            paneelTypeTellingen: [new OverzichtGroeperingHelper.Telling("Front", 3)],
            toewijzingenAantal: 3,
            geopendeWandId: wandB.Wand.Id,
            isReviewWeergaveActief: false,
            toonEditorDrawer: false,
            paneelRandSpeling: 3,
            paneelEditorStatus: MaakEditorStatus());

        Assert.True(model.ToonCompacteStapIntro);
        Assert.NotNull(model.ActieveWerkruimte);
        Assert.Equal("Rechterwand", model.ActieveWerkruimte!.Samenvatting.Werkruimte.Wand.Naam);
        Assert.Single(model.OverzichtWanden);
        Assert.Equal("Linkerwand", model.OverzichtWanden[0].Werkruimte.Wand.Naam);
        Assert.Equal("Rechterwand", model.ReviewGroepen[0].WandNaam);
    }

    [Fact]
    public void BouwPaginaModel_bouwt_kaart_en_schakelaar_teksten_voor_lege_wand()
    {
        var wand = MaakWerkruimte("Lege wand", aantalKasten: 0, aantalPanelen: 0);

        var model = PaneelConfiguratieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wandWerkruimtes: [wand],
            paneelReviewGroepen: [],
            paneelTypeTellingen: [],
            toewijzingenAantal: 0,
            geopendeWandId: null,
            isReviewWeergaveActief: false,
            toonEditorDrawer: false,
            paneelRandSpeling: 3,
            paneelEditorStatus: MaakEditorStatus());

        var samenvatting = Assert.Single(model.OverzichtWanden);
        Assert.Equal("Eerst stap 1", samenvatting.OverzichtKnopLabel);
        Assert.Equal("btn btn-outline-secondary", samenvatting.OverzichtKnopClass);
        Assert.True(samenvatting.OverzichtKnopUitgeschakeld);
        Assert.Equal("Voeg eerst een kast toe in stap 1.", samenvatting.OverzichtBeschrijving);
        Assert.Equal("Eerst kast toevoegen", samenvatting.SchakelaarKnopLabel);
        Assert.True(samenvatting.SchakelaarKnopUitgeschakeld);
        Assert.Equal("Nog geen kasten op deze wand. Voeg die eerst in stap 1 toe.", samenvatting.SchakelaarBeschrijving);
        Assert.Equal([$"{wand.Wand.Breedte:0.#} mm wand"], samenvatting.OverzichtMetaItems);
    }

    [Fact]
    public void BouwPaginaModel_maakt_compacte_tabs_en_actieve_werkruimte_status()
    {
        var wand = MaakWerkruimte("Achterwand", aantalKasten: 2, aantalPanelen: 2);
        var status = MaakEditorStatus(
            volgendeStapTekst: "Controleer paneelmaat",
            werkruimteStatusDetailTekst: "2 kast(en) geselecteerd. Open de editor voor plaatsing, maat en opslaan.");

        var editorOpen = PaneelConfiguratieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wandWerkruimtes: [wand],
            paneelReviewGroepen: [],
            paneelTypeTellingen: [],
            toewijzingenAantal: 2,
            geopendeWandId: wand.Wand.Id,
            isReviewWeergaveActief: false,
            toonEditorDrawer: true,
            paneelRandSpeling: 3,
            paneelEditorStatus: status);
        var editorGesloten = PaneelConfiguratieReadModelHelper.BouwPaginaModel(
            routeGate: null,
            wandWerkruimtes: [wand],
            paneelReviewGroepen: [],
            paneelTypeTellingen: [],
            toewijzingenAantal: 2,
            geopendeWandId: wand.Wand.Id,
            isReviewWeergaveActief: false,
            toonEditorDrawer: false,
            paneelRandSpeling: 4,
            paneelEditorStatus: status);

        Assert.True(editorOpen.ToonCompacteWeergaveTabs);
        Assert.True(editorOpen.WeergaveTabs.IsCompact);
        Assert.Equal("Editor open voor Achterwand", editorOpen.WeergaveTabs.Titel);
        Assert.Equal(["2 paneel/panelen klaar"], editorOpen.WeergaveTabs.MetaItems);
        Assert.Null(editorOpen.ActieveWerkruimte!.WerkruimteStatus);

        Assert.False(editorGesloten.ToonCompacteWeergaveTabs);
        Assert.True(editorGesloten.ToonUitgeklapteProjectinstellingen);
        Assert.NotNull(editorGesloten.ActieveWerkruimte!.WerkruimteStatus);
        Assert.Equal("Controleer paneelmaat", editorGesloten.ActieveWerkruimte.WerkruimteStatus!.Titel);
        Assert.Equal("Editor gesloten", editorGesloten.ActieveWerkruimte.MetaItems[^1]);
    }

    private static PaneelWerkruimteContext MaakWerkruimte(string wandNaam, int aantalKasten, int aantalPanelen)
    {
        var wand = new KeukenWand
        {
            Id = Guid.NewGuid(),
            Naam = wandNaam,
            Breedte = 2400,
            Hoogte = 2700
        };

        var kasten = Enumerable.Range(1, aantalKasten)
            .Select(index => new Kast
            {
                Id = Guid.NewGuid(),
                Naam = $"Kast {index}",
                Breedte = 600,
                Hoogte = 720,
                XPositie = (index - 1) * 600,
                HoogteVanVloer = 0
            })
            .ToList();
        var toewijzingen = Enumerable.Range(1, aantalPanelen)
            .Select(index => new PaneelToewijzing
            {
                Id = Guid.NewGuid(),
                Type = PaneelType.BlindPaneel,
                KastIds = kasten.Take(Math.Max(1, Math.Min(kasten.Count, 1))).Select(kast => kast.Id).ToList(),
                Breedte = 600,
                Hoogte = 720,
                XPositie = 0,
                HoogteVanVloer = (index - 1) * 720
            })
            .ToList();

        return new PaneelWerkruimteContext(wand, kasten, [], toewijzingen, []);
    }

    private static OverzichtGroeperingHelper.PaneelReviewGroep MaakReviewGroep(KeukenWand wand, int aantalPanelen)
        => new(
            wand.Id,
            wand.Naam,
            aantalPanelen,
            [new OverzichtGroeperingHelper.Telling("Front", aantalPanelen)],
            []);

    private static PaneelEditorStatusModel MaakEditorStatus(
        string volgendeStapTekst = "Selecteer eerst kast(en) in de tekening",
        string werkruimteStatusDetailTekst = "Selecteer eerst kast(en) in de tekening.")
        => new(
            GeopendeWandNaam: "Paneel-editor",
            ToonCompacteEditorLeegstaat: true,
            IsBewerkModus: false,
            HeeftEnkeleKastSelectie: false,
            KanKastOpdelen: false,
            KanOpslaan: false,
            EditorDrawerTitel: "Paneel-editor",
            EditorHeaderMeta: null,
            VolgendePaneelStapTekst: volgendeStapTekst,
            KernHintTekst: string.Empty,
            SelectieSamenvatting: "Nog geen kast",
            OpslaanSamenvatting: "Nog controleren",
            OpslaanStatusTekst: "Nee, er is nog geen actieve wand.",
            WerklaagStatusTekst: "Selecteer eerst kast(en) in de tekening",
            WerkruimteStatusDetailTekst: werkruimteStatusDetailTekst,
            OpenEditorKnopLabel: "Open paneel-editor",
            OpdeelStatusClass: "alert-warning",
            OpdeelStatusTekst: "Nog 720 mm te verdelen.");
}
