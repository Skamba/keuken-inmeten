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

        if (toonKastOpdelenModal && !KanKastOpdelen)
            toonKastOpdelenModal = false;

        _ = InvokeAsync(StateHasChanged);
    }

    private bool IsBewerkModus => bewerkToewijzingId is not null;
    private PaneelSelectieContext HuidigeSelectieContext => BouwSelectieContext();
    private bool ToonCompacteEditorLeegstaat => !IsBewerkModus && !HuidigeSelectieContext.HeeftSelectie;
    private bool HeeftEnkeleKastSelectie => !IsBewerkModus && HuidigeSelectieContext.HeeftEnkeleKastSelectie;
    private bool KanKastOpdelen
        => HuidigeSelectieContext.OpdeelBereik is { Hoogte: var hoogte }
           && hoogte >= (2 * PaneelLayoutService.MinPaneelMaat) - 0.001;

    private GeindexeerdeToewijzing? BewerkteToewijzingInfo
        => bewerkToewijzingId is Guid id ? State.ZoekGeindexeerdeToewijzing(id) : null;

    private KeukenWand? GeopendeWand
        => geopendeWandId is Guid id ? State.ZoekWand(id) : null;

    private int BewerkIndex =>
        BewerkteToewijzingInfo is { Index: var index } ? index + 1 : 0;

    private PaneelToewijzing? BewerkteToewijzing =>
        BewerkteToewijzingInfo?.Toewijzing;

    private PaneelRechthoek? OpdeelBereik => HuidigeSelectieContext.OpdeelBereik;

    private PaneelOpdeelAnalyse OpdeelAnalyse
        => PaneelConfiguratieHelper.AnalyseerOpdeelHoogtes(HuidigeSelectieContext.OpdeelBereik?.Hoogte ?? 0, opdeelHoogtes);

    private PaneelFlowContext HuidigePaneelFlow
    {
        get
        {
            var selectieContext = HuidigeSelectieContext;
            return new PaneelFlowContext(
                HeeftWandContext: geopendeWandId is not null,
                HeeftSelectie: selectieContext.HeeftSelectie,
                HeeftConceptPaneel: conceptPaneel is not null,
                HeeftGeldigeMaat: formToewijzing.Breedte > 0 && formToewijzing.Hoogte > 0,
                RaaktGeselecteerdeKast: RaaktGeselecteerdeKast(),
                HeeftConflicterendPaneel: HeeftConflicterendPaneel(),
                ActieveWandNaam: GeopendeWand?.Naam ?? selectieContext.ActieveWandNaam,
                GeselecteerdeKastNamen: selectieContext.GeselecteerdeKastNamen);
        }
    }

    private PaneelEditorStatusModel HuidigePaneelEditorStatus
    {
        get
        {
            var selectieContext = HuidigeSelectieContext;
            return PaneelConfiguratieHelper.BouwEditorStatus(new PaneelEditorStatusContext(
                Flow: HuidigePaneelFlow,
                GeopendeWandNaam: GeopendeWand?.Naam ?? selectieContext.ActieveWandNaam,
                GeselecteerdeKastAantal: selectieContext.GeselecteerdeKasten.Count,
                ToonEditorDrawer: toonEditorDrawer,
                ToonCompacteEditorLeegstaat: ToonCompacteEditorLeegstaat,
                IsBewerkModus: IsBewerkModus,
                HeeftEnkeleKastSelectie: HeeftEnkeleKastSelectie,
                KanKastOpdelen: KanKastOpdelen,
                BewerkIndex: BewerkIndex,
                OpdeelAnalyse: OpdeelAnalyse));
        }
    }

    private PaneelSelectieContext BouwSelectieContext()
    {
        var actieveWandId = BepaalActieveWandId();
        var geselecteerdeKasten = State.ZoekKasten(geselecteerdeKastIds);
        var actieveWandNaam = actieveWandId is Guid wandId
            ? State.ZoekWand(wandId)?.Naam ?? "—"
            : "—";
        var werkruimte = actieveWandId is Guid wandContextId
            ? State.LeesPaneelWerkruimte(wandContextId, bewerkToewijzingId)
            : null;

        return PaneelConfiguratieHelper.BouwSelectieContext(
            actieveWandId,
            actieveWandNaam,
            geselecteerdeKasten,
            werkruimte,
            IsBewerkModus);
    }

    private Guid? BepaalActieveWandId()
    {
        var wandIds = geselecteerdeKastIds
            .Select(VindWandId)
            .Where(id => id is not null)
            .Distinct()
            .Cast<Guid>()
            .ToList();

        return wandIds.Count == 1 ? wandIds[0] : null;
    }

    private bool RaaktGeselecteerdeKast()
    {
        var selectieContext = HuidigeSelectieContext;
        return conceptPaneel is not null
            && PaneelLayoutService.BepaalOverlappendeKasten(selectieContext.GeselecteerdeKasten, conceptPaneel).Count > 0;
    }

    private bool HeeftConflicterendPaneel()
    {
        var selectieContext = HuidigeSelectieContext;
        return conceptPaneel is not null
            && selectieContext.BestaandePaneelRechthoeken.Any(bestaandPaneel => PaneelLayoutService.HeeftOverlap(conceptPaneel, bestaandPaneel));
    }

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
