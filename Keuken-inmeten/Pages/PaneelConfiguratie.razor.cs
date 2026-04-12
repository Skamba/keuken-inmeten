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
        if (geopendeWandId is Guid wandId && State.Wanden.All(wand => wand.Id != wandId))
        {
            geopendeWandId = null;
            toonEditorDrawer = false;
            bewerkToewijzingId = null;
            geselecteerdeKastIds.Clear();
            conceptPaneel = null;
            ResetFormToewijzing();
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

    private KeukenWand? GeopendeWand
        => geopendeWandId is Guid id ? State.Wanden.FirstOrDefault(wand => wand.Id == id) : null;

    private int BewerkIndex =>
        bewerkToewijzingId is Guid id
            ? State.Toewijzingen.FindIndex(toewijzing => toewijzing.Id == id) + 1
            : 0;

    private PaneelToewijzing? BewerkteToewijzing =>
        bewerkToewijzingId is Guid id
            ? State.Toewijzingen.FirstOrDefault(toewijzing => toewijzing.Id == id)
            : null;

    private List<Kast> geselecteerdeKasten =>
        geselecteerdeKastIds
            .Select(id => State.Kasten.Find(k => k.Id == id))
            .Where(k => k is not null)
            .Cast<Kast>()
            .ToList();

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

    private string ActieveWandNaam =>
        ActieveWandId is Guid wandId ? State.Wanden.Find(w => w.Id == wandId)?.Naam ?? "—" : "—";

    private PaneelFlowContext HuidigePaneelFlow => new(
        HeeftWandContext: geopendeWandId is not null,
        HeeftSelectie: geselecteerdeKastIds.Count > 0,
        HeeftConceptPaneel: conceptPaneel is not null,
        HeeftGeldigeMaat: formToewijzing.Breedte > 0 && formToewijzing.Hoogte > 0,
        RaaktGeselecteerdeKast: RaaktGeselecteerdeKast(),
        HeeftConflicterendPaneel: HeeftConflicterendPaneel(),
        ActieveWandNaam: GeopendeWand?.Naam ?? ActieveWandNaam,
        GeselecteerdeKastNamen: string.Join(" + ", geselecteerdeKasten.Select(kast => kast.Naam)));

    private bool KanPaneelOpslaan()
        => PaneelConfiguratieHelper.KanPaneelOpslaan(HuidigePaneelFlow);

    private string VolgendePaneelStapTekst()
        => PaneelConfiguratieHelper.BepaalVolgendePaneelStapTekst(HuidigePaneelFlow);

    private string OpslaanStatusTekst()
        => PaneelConfiguratieHelper.BepaalOpslaanStatusTekst(HuidigePaneelFlow);

    private bool RaaktGeselecteerdeKast()
        => conceptPaneel is not null
            && PaneelLayoutService.BepaalOverlappendeKasten(geselecteerdeKasten, conceptPaneel).Count > 0;

    private bool HeeftConflicterendPaneel()
        => conceptPaneel is not null
            && BestaandePaneelRechthoeken().Any(bestaandPaneel => PaneelLayoutService.HeeftOverlap(conceptPaneel, bestaandPaneel));

    private string PaneelEditorSelectieSamenvatting()
        => geselecteerdeKasten.Count switch
        {
            0 => "Nog geen kast",
            1 => geselecteerdeKasten[0].Naam,
            _ => $"{geselecteerdeKasten.Count} kasten"
        };

    private static string PaneelEditorOpslaanSamenvatting(bool kanOpslaan)
        => kanOpslaan ? "Klaar" : "Nog controleren";

    private string PaneelEditorKernHintTekst(bool kanOpslaan)
    {
        if (kanOpslaan)
            return "Controleer hieronder alleen nog maat en type. Daarna kunt u direct opslaan.";

        if (conceptPaneel is null)
            return "Sleep in de tekening of kies een vrij vak. De velden hieronder volgen direct mee.";

        if (HeeftConflicterendPaneel())
            return "Verplaats of verklein het paneel totdat het geen bestaand paneel meer overlapt.";

        return RaaktGeselecteerdeKast()
            ? "Controleer hieronder maat en type voordat u opslaat."
            : "Pas positie, maat of selectie aan totdat het paneel weer een geselecteerde kast raakt.";
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
