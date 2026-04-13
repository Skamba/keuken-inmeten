using Microsoft.AspNetCore.Components;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class PaneelConfiguratie
{
    private PaneelToewijzing formToewijzing = new();
    private readonly HashSet<Guid> geselecteerdeKastIds = [];
    private PaneelRechthoek? conceptPaneel;
    private Guid? bewerkToewijzingId;
    private Guid? geopendeWandId;
    private bool toonEditorDrawer;
    private bool toonKastOpdelenModal;
    private int opdeelAantal = 2;
    private List<double> opdeelHoogtes = [];
    private bool reviewWeergaveActief;

    protected override void OnInitialized()
    {
        State.OnStateChanged += HandleStateChanged;
        ResetFormToewijzing();
    }

    public void Dispose()
        => State.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged()
    {
        if (geopendeWandId is Guid wandId && State.ZoekWand(wandId) is null)
        {
            geopendeWandId = null;
            SluitPaneelWerklaag();
            VerlaatPaneelBewerkmodus(resetFormulier: true);
            WisGeselecteerdeKasten();
            conceptPaneel = null;
        }

        if (reviewWeergaveActief && State.Toewijzingen.Count == 0)
            reviewWeergaveActief = false;

        if (toonKastOpdelenModal && !KanKastOpdelen)
            toonKastOpdelenModal = false;

        _ = InvokeAsync(StateHasChanged);
    }

    private bool IsBewerkModus => bewerkToewijzingId is not null;
    private bool IsReviewWeergaveActief => reviewWeergaveActief && State.Toewijzingen.Count > 0;
    private bool ToonCompacteEditorLeegstaat => !IsBewerkModus && geselecteerdeKastIds.Count == 0;
    private bool HeeftEnkeleKastSelectie => !IsBewerkModus && geselecteerdeKasten.Count == 1;
    private bool KanKastOpdelen
        => OpdeelBereik is { Hoogte: var hoogte }
           && hoogte >= (2 * PaneelLayoutService.MinPaneelMaat) - 0.001;

    private GeindexeerdeToewijzing? BewerkteToewijzingInfo
        => bewerkToewijzingId is Guid id ? State.ZoekGeindexeerdeToewijzing(id) : null;

    private KeukenWand? GeopendeWand
        => geopendeWandId is Guid id ? State.ZoekWand(id) : null;

    private int BewerkIndex =>
        BewerkteToewijzingInfo is { Index: var index } ? index + 1 : 0;

    private PaneelToewijzing? BewerkteToewijzing =>
        BewerkteToewijzingInfo?.Toewijzing;

    private List<Kast> geselecteerdeKasten =>
        State.ZoekKasten(geselecteerdeKastIds);

    private PaneelRechthoek? OpdeelBereik
    {
        get
        {
            if (!HeeftEnkeleKastSelectie)
                return null;

            var selectieBereik = PaneelLayoutService.BerekenOmhullende(geselecteerdeKasten);
            return selectieBereik is null
                ? null
                : PaneelConfiguratieHelper.BepaalOpdeelBereik(selectieBereik, BestaandePaneelRechthoeken());
        }
    }

    private PaneelOpdeelAnalyse OpdeelAnalyse
        => PaneelConfiguratieHelper.AnalyseerOpdeelHoogtes(OpdeelBereik?.Hoogte ?? 0, opdeelHoogtes);

    private Guid? ActieveWandId
    {
        get
        {
            var wandIds = geselecteerdeKastIds
                .Select(VindWandId)
                .Where(id => id is not null)
                .Distinct()
                .Cast<Guid>()
                .ToList();

            return wandIds.Count == 1 ? wandIds[0] : null;
        }
    }

    private PaneelWerkruimteContext? ActievePaneelWerkruimte
        => ActieveWandId is Guid wandId ? State.LeesPaneelWerkruimte(wandId, bewerkToewijzingId) : null;

    private string ActieveWandNaam =>
        ActieveWandId is Guid wandId ? State.ZoekWand(wandId)?.Naam ?? "—" : "—";

    private PaneelFlowContext HuidigePaneelFlow => new(
        HeeftWandContext: geopendeWandId is not null,
        HeeftSelectie: geselecteerdeKastIds.Count > 0,
        HeeftConceptPaneel: conceptPaneel is not null,
        HeeftGeldigeMaat: formToewijzing.Breedte > 0 && formToewijzing.Hoogte > 0,
        RaaktGeselecteerdeKast: RaaktGeselecteerdeKast(),
        HeeftConflicterendPaneel: HeeftConflicterendPaneel(),
        ActieveWandNaam: GeopendeWand?.Naam ?? ActieveWandNaam,
        GeselecteerdeKastNamen: string.Join(" + ", geselecteerdeKasten.Select(kast => kast.Naam)));

    private PaneelEditorStatusModel HuidigePaneelEditorStatus
        => PaneelConfiguratieHelper.BouwEditorStatus(new PaneelEditorStatusContext(
            Flow: HuidigePaneelFlow,
            GeopendeWandNaam: GeopendeWand?.Naam ?? ActieveWandNaam,
            GeselecteerdeKastAantal: geselecteerdeKastIds.Count,
            ToonEditorDrawer: toonEditorDrawer,
            ToonCompacteEditorLeegstaat: ToonCompacteEditorLeegstaat,
            IsBewerkModus: IsBewerkModus,
            HeeftEnkeleKastSelectie: HeeftEnkeleKastSelectie,
            KanKastOpdelen: KanKastOpdelen,
            BewerkIndex: BewerkIndex,
            OpdeelAnalyse: OpdeelAnalyse));

    private bool RaaktGeselecteerdeKast()
        => conceptPaneel is not null
            && PaneelLayoutService.BepaalOverlappendeKasten(geselecteerdeKasten, conceptPaneel).Count > 0;

    private bool HeeftConflicterendPaneel()
        => conceptPaneel is not null
            && BestaandePaneelRechthoeken().Any(bestaandPaneel => PaneelLayoutService.HeeftOverlap(conceptPaneel, bestaandPaneel));

    private double BreedteInput
    {
        get => conceptPaneel?.Breedte ?? 0;
        set
        {
            if (conceptPaneel is null) return;
            var voorstel = conceptPaneel.Kopie();
            voorstel.Breedte = value;
            VerwerkConceptPaneel(new PaneelConceptWijziging { Bewerking = "input-size", Paneel = voorstel });
        }
    }

    private double HoogteInput
    {
        get => conceptPaneel?.Hoogte ?? 0;
        set
        {
            if (conceptPaneel is null) return;
            var voorstel = conceptPaneel.Kopie();
            voorstel.Hoogte = value;
            VerwerkConceptPaneel(new PaneelConceptWijziging { Bewerking = "input-size", Paneel = voorstel });
        }
    }

    private double LinksInput
    {
        get => conceptPaneel?.XPositie ?? 0;
        set
        {
            if (conceptPaneel is null) return;
            var voorstel = conceptPaneel.Kopie();
            voorstel.XPositie = value;
            VerwerkConceptPaneel(new PaneelConceptWijziging { Bewerking = "input-position", Paneel = voorstel });
        }
    }

    private double OnderkantInput
    {
        get => conceptPaneel?.HoogteVanVloer ?? 0;
        set
        {
            if (conceptPaneel is null) return;
            var voorstel = conceptPaneel.Kopie();
            voorstel.HoogteVanVloer = value;
            VerwerkConceptPaneel(new PaneelConceptWijziging { Bewerking = "input-position", Paneel = voorstel });
        }
    }

    private double PotHartInput
    {
        get => formToewijzing.PotHartVanRand;
        set => formToewijzing.PotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(value);
    }

    private double ProjectRandSpelingInput
    {
        get => State.PaneelRandSpeling;
        set => State.StelPaneelRandSpelingIn(value);
    }
}
