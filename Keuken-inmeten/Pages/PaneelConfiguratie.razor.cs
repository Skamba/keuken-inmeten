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

    private void ToggleKast(Guid kastId)
    {
        var wandId = VindWandId(kastId);
        if (wandId is null)
            return;

        reviewWeergaveActief = false;
        geopendeWandId = wandId;
        toonEditorDrawer = true;
        bewerkToewijzingId = null;

        if (ActieveWandId is Guid actieveWandId && actieveWandId != wandId)
            geselecteerdeKastIds.Clear();

        if (!geselecteerdeKastIds.Add(kastId))
            geselecteerdeKastIds.Remove(kastId);

        ResetConceptPaneel();
    }

    private void DeselecteerWand(Guid wandId)
    {
        bewerkToewijzingId = null;
        if (geopendeWandId == wandId)
        {
            reviewWeergaveActief = false;
            geopendeWandId = null;
            toonEditorDrawer = false;
        }

        var wandKastIds = State.KastenVoorWand(wandId).Select(k => k.Id).ToList();
        foreach (var id in wandKastIds)
            geselecteerdeKastIds.Remove(id);

        ResetFormToewijzing();
        ResetConceptPaneel();
    }

    private void ToggleWandWerkruimte(Guid wandId)
    {
        if (geopendeWandId == wandId)
        {
            DeselecteerWand(wandId);
            return;
        }

        OpenWandWerkruimte(wandId);
    }

    private void DeselecteerAlles()
    {
        bewerkToewijzingId = null;
        geselecteerdeKastIds.Clear();
        ResetFormToewijzing();
        ResetConceptPaneel();
    }

    private void OpenWandWerkruimte(Guid wandId)
    {
        if (geopendeWandId != wandId)
        {
            bewerkToewijzingId = null;
            geselecteerdeKastIds.Clear();
            ResetFormToewijzing();
        }

        reviewWeergaveActief = false;
        geopendeWandId = wandId;
        toonEditorDrawer = false;
        ResetConceptPaneel();
    }

    private void SluitWandWerkruimte()
    {
        geopendeWandId = null;
        toonEditorDrawer = false;
        bewerkToewijzingId = null;
        geselecteerdeKastIds.Clear();
        ResetFormToewijzing();
        ResetConceptPaneel();
    }

    private void ActiveerEditorWeergave() => reviewWeergaveActief = false;

    private void ActiveerReviewWeergave()
    {
        if (State.Toewijzingen.Count == 0)
            return;

        reviewWeergaveActief = true;
        toonEditorDrawer = false;
        toonKastOpdelenModal = false;
    }

    private void OpenEditorDrawer()
    {
        if (geopendeWandId is not null)
        {
            reviewWeergaveActief = false;
            toonEditorDrawer = true;
        }
    }

    private void SluitEditorDrawer()
    {
        toonEditorDrawer = false;
        bewerkToewijzingId = null;
        ResetFormToewijzing();
        ResetConceptPaneel();
    }

    private string EditorDrawerTitel()
        => IsBewerkModus
            ? $"Paneel bewerken — {GeopendeWand?.Naam}"
            : ToonCompacteEditorLeegstaat
                ? $"Kastselectie — {GeopendeWand?.Naam}"
                : $"Paneel plaatsen — {GeopendeWand?.Naam}";

    private string EditorWerklaagStatusTekst()
    {
        if (toonEditorDrawer)
            return ToonCompacteEditorLeegstaat ? "Editor open; selecteer nu kast(en)" : "Paneel-editor staat open";

        return geselecteerdeKastIds.Count > 0
            ? "Selectie klaar; open nu de editor"
            : "Selecteer eerst kast(en) in de tekening";
    }

    private string EditorStatusHintTekst()
        => toonEditorDrawer
            ? ToonCompacteEditorLeegstaat
                ? "Zodra u kast(en) kiest, verschijnen plaatsing, maat en opslaan hier."
                : "Plaatsing, maat en opslaan staan nu in de editorlaag."
            : geselecteerdeKastIds.Count > 0
                ? "Open de editor voor plaatsing, maat en opslaan."
                : "Selecteer eerst kast(en) in de tekening.";

    private string OpenEditorKnopLabel()
        => geselecteerdeKastIds.Count > 0 || IsBewerkModus ? "Open paneel-editor" : "Open editorlaag";

    private string PaneelWerkruimteStatusDetailTekst()
        => geselecteerdeKastIds.Count > 0
            ? $"{geselecteerdeKastIds.Count} kast(en) geselecteerd. {EditorStatusHintTekst()}"
            : EditorStatusHintTekst();

    private bool KanKastOpdelenIn(int aantal)
        => OpdeelBereik is { Hoogte: var hoogte }
           && hoogte >= (aantal * PaneelLayoutService.MinPaneelMaat) - 0.001;

    private string OpdeelInstellingenTekst()
        => formToewijzing.Type == PaneelType.Deur
            ? $"{TypeNaam(formToewijzing.Type)} · scharnier {formToewijzing.ScharnierZijde.ToString().ToLowerInvariant()} · pot-hart {FormatMm(PotHartInput)}"
            : TypeNaam(formToewijzing.Type);

    private string OpdeelStatusTekst()
    {
        if (!OpdeelAnalyse.HeeftGeldigeDeelHoogtes)
            return $"Elk deel moet minimaal {FormatMm(PaneelLayoutService.MinPaneelMaat)} hoog zijn.";

        if (OpdeelAnalyse.KanBevestigen)
            return "De ingevulde hoogtes vullen de volledige opening.";

        return OpdeelAnalyse.RestantHoogte > 0
            ? $"Nog {FormatMm(OpdeelAnalyse.RestantHoogte)} te verdelen."
            : $"{FormatMm(Math.Abs(OpdeelAnalyse.RestantHoogte))} te veel ingevuld.";
    }

    private string OpdeelStatusClass()
        => OpdeelAnalyse.KanBevestigen
            ? "alert-success"
            : OpdeelAnalyse.HeeftGeldigeDeelHoogtes
                ? "alert-warning"
                : "alert-danger";

    private void OpenKastOpdelenModal()
    {
        if (!KanKastOpdelen)
            return;

        StelOpdeelAantalIn(KanKastOpdelenIn(opdeelAantal) ? opdeelAantal : 2);
        toonKastOpdelenModal = true;
    }

    private void SluitKastOpdelenModal() => toonKastOpdelenModal = false;

    private void StelOpdeelAantalIn(int aantal)
    {
        if (!KanKastOpdelenIn(aantal) || OpdeelBereik is not { } bereik)
            return;

        opdeelAantal = aantal;
        opdeelHoogtes = MaakStandaardOpdeelHoogtes(bereik.Hoogte, aantal);
    }

    private void BevestigKastOpdelen()
    {
        if (OpdeelBereik is not { } bereik || geselecteerdeKasten.Count != 1)
            return;

        var analyse = PaneelConfiguratieHelper.AnalyseerOpdeelHoogtes(bereik.Hoogte, opdeelHoogtes);
        if (!analyse.KanBevestigen)
            return;

        var kast = geselecteerdeKasten[0];
        var toewijzingen = PaneelConfiguratieHelper.BouwOpdeelSegmenten(bereik, opdeelHoogtes)
            .Select(segment => MaakPaneelToewijzing(segment, [kast]))
            .ToList();

        State.VoegToewijzingenToe(toewijzingen);
        Feedback.ToonSucces($"{toewijzingen.Count} panelen toegevoegd voor '{kast.Naam}'.");
        RondPaneelInvoerAf();
    }

    private void ResetConceptPaneel()
    {
        toonKastOpdelenModal = false;

        if (BewerkteToewijzing is { } bewerkteToewijzing)
        {
            LaadToewijzingInFormulier(bewerkteToewijzing);
            return;
        }

        var selectieBereik = PaneelLayoutService.BerekenOmhullende(geselecteerdeKasten);
        if (selectieBereik is null)
        {
            conceptPaneel = null;
            formToewijzing.XPositie = null;
            formToewijzing.HoogteVanVloer = null;
            formToewijzing.Breedte = 0;
            formToewijzing.Hoogte = 0;
            return;
        }

        conceptPaneel = BepaalStartRechthoek(selectieBereik);
        UpdateFormVanConcept();
    }

    private void ResetFormToewijzing()
        => formToewijzing = IndelingFormulierHelper.NieuwePaneelToewijzing(State.LaatstGebruiktePotHartVanRand);

    private void LaadToewijzingInFormulier(PaneelToewijzing toewijzing)
    {
        var paneelBron = PaneelBron(toewijzing);
        if (paneelBron is null)
        {
            bewerkToewijzingId = null;
            ResetConceptPaneel();
            return;
        }

        ResetFormToewijzing();
        formToewijzing.Type = toewijzing.Type;
        formToewijzing.ScharnierZijde = toewijzing.ScharnierZijde;
        formToewijzing.PotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(toewijzing.PotHartVanRand);
        conceptPaneel = paneelBron.OpeningsRechthoek.Kopie();
        UpdateFormVanConcept();
    }

    private void UpdateFormVanConcept()
    {
        if (conceptPaneel is null)
            return;

        formToewijzing.XPositie = Math.Round(conceptPaneel.XPositie, 1);
        formToewijzing.HoogteVanVloer = Math.Round(conceptPaneel.HoogteVanVloer, 1);
        formToewijzing.Breedte = Math.Round(conceptPaneel.Breedte, 1);
        formToewijzing.Hoogte = Math.Round(conceptPaneel.Hoogte, 1);
    }

    private void VerwerkConceptPaneel(PaneelConceptWijziging wijziging)
    {
        var selectieBereik = PaneelLayoutService.BerekenOmhullende(geselecteerdeKasten);
        if (selectieBereik is null)
            return;

        var voorstel = PaneelLayoutService.ClampBinnen(wijziging.Paneel, selectieBereik);
        conceptPaneel = wijziging.Bewerking.StartsWith("input-", StringComparison.Ordinal)
            ? voorstel
            : SnapPaneel(wijziging.Bewerking, voorstel, selectieBereik);
        UpdateFormVanConcept();
    }

    private PaneelRechthoek SnapPaneel(string bewerking, PaneelRechthoek voorstel, PaneelRechthoek selectieBereik)
        => PaneelConfiguratieHelper.SnapPaneel(
            bewerking,
            voorstel,
            selectieBereik,
            XTargets(selectieBereik),
            YTargets(selectieBereik));

    private IEnumerable<double> XTargets(PaneelRechthoek selectieBereik)
    {
        yield return selectieBereik.XPositie;
        yield return selectieBereik.Rechterkant;

        foreach (var kast in geselecteerdeKasten)
        {
            yield return kast.XPositie;
            yield return kast.XPositie + kast.Breedte;
        }

        foreach (var paneel in BestaandePaneelRechthoeken())
        {
            yield return paneel.XPositie;
            yield return paneel.Rechterkant;
        }
    }

    private IEnumerable<double> YTargets(PaneelRechthoek selectieBereik)
    {
        yield return selectieBereik.HoogteVanVloer;
        yield return selectieBereik.Bovenzijde;

        foreach (var kast in geselecteerdeKasten)
        {
            yield return kast.HoogteVanVloer;
            yield return kast.HoogteVanVloer + kast.Hoogte;
        }

        foreach (var paneel in BestaandePaneelRechthoeken())
        {
            yield return paneel.HoogteVanVloer;
            yield return paneel.Bovenzijde;
        }
    }

    private IEnumerable<PaneelRechthoek> BestaandePaneelRechthoeken()
    {
        foreach (var paneelBron in PaneelBronnenVoorActieveWand())
        {
            if (paneelBron.PaneelId == bewerkToewijzingId)
                continue;

            yield return paneelBron.OpeningsRechthoek.Kopie();
        }
    }

    private List<PaneelRechthoek> VrijeSegmentenVoorSelectie()
    {
        var selectieBereik = PaneelLayoutService.BerekenOmhullende(geselecteerdeKasten);
        return selectieBereik is null
            ? []
            : PaneelConfiguratieHelper.BepaalVrijeSegmenten(selectieBereik, BestaandePaneelRechthoeken());
    }

    private PaneelRechthoek BepaalStartRechthoek(PaneelRechthoek selectieBereik)
        => PaneelConfiguratieHelper.BepaalStartRechthoek(selectieBereik, VrijeSegmentenVoorSelectie());

    private void GebruikVrijSegment(PaneelRechthoek segment)
    {
        conceptPaneel = segment.Kopie();
        UpdateFormVanConcept();
    }

    private PaneelMaatInfo? BerekenConceptMaatInfo()
    {
        if (conceptPaneel is null)
            return null;

        if (ActieveWandId is not Guid wandId)
            return null;

        var wandKasten = State.KastenVoorWand(wandId);
        var paneelBronnen = PaneelBronnenVoorActieveWand();
        return PaneelGeometrieService.BerekenVoorConceptPaneel(
            conceptPaneel,
            wandKasten,
            State.ApparatenVoorWand(wandId),
            paneelBronnen,
            State.PaneelRandSpeling,
            bewerkToewijzingId)?.MaatInfo;
    }

    private void PaneelOpslaan()
    {
        if (conceptPaneel is null || geselecteerdeKastIds.Count == 0)
            return;

        if (HeeftConflicterendPaneel())
        {
            Feedback.ToonFout("Dit paneel overlapt nog een bestaand paneel. Verplaats of verklein het eerst.");
            return;
        }

        var dragendeKasten = PaneelLayoutService.BepaalOverlappendeKasten(geselecteerdeKasten, conceptPaneel);
        if (dragendeKasten.Count == 0)
            return;

        var toewijzing = MaakPaneelToewijzing(conceptPaneel, dragendeKasten, bewerkToewijzingId);

        if (IsBewerkModus)
            State.WerkToewijzingBij(toewijzing);
        else
            State.VoegToewijzingToe(toewijzing);

        RondPaneelInvoerAf();
    }

    private List<PaneelToewijzing> ToewijzingenVoorWand(KeukenWand wand)
    {
        var wandKastIds = wand.KastIds.ToHashSet();
        return State.Toewijzingen
            .Where(t => t.Id != bewerkToewijzingId && t.KastIds.Any(wandKastIds.Contains))
            .ToList();
    }

    private PaneelGeometrieBron? PaneelBron(PaneelToewijzing toewijzing)
        => PaneelGeometrieService.MaakBronVoorToewijzing(toewijzing, State.ZoekKasten(toewijzing.KastIds));

    private List<PaneelGeometrieBron> PaneelBronnenVoorActieveWand()
    {
        if (ActieveWandId is not Guid actieveWandId)
            return [];

        var wandKastIds = State.KastenVoorWand(actieveWandId).Select(kast => kast.Id).ToHashSet();
        return State.Toewijzingen
            .Where(toewijzing => toewijzing.KastIds.Any(wandKastIds.Contains))
            .Select(PaneelBron)
            .Where(bron => bron is not null)
            .Cast<PaneelGeometrieBron>()
            .ToList();
    }

    private void BewerkPaneel(Guid toewijzingId)
    {
        var toewijzing = State.Toewijzingen.FirstOrDefault(item => item.Id == toewijzingId);
        if (toewijzing is null)
            return;

        reviewWeergaveActief = false;
        geopendeWandId = VindWandIdVoorToewijzing(toewijzing);
        toonEditorDrawer = true;
        bewerkToewijzingId = toewijzingId;
        geselecteerdeKastIds.Clear();
        foreach (var kastId in toewijzing.KastIds)
            geselecteerdeKastIds.Add(kastId);

        ResetConceptPaneel();
    }

    private void AnnuleerBewerken()
    {
        bewerkToewijzingId = null;
        ResetFormToewijzing();
        ResetConceptPaneel();
    }

    private void VerwijderPaneel(Guid toewijzingId)
    {
        var index = State.Toewijzingen.FindIndex(item => item.Id == toewijzingId);
        var toewijzing = index >= 0 ? State.Toewijzingen[index] : null;
        if (toewijzing is null)
            return;

        if (bewerkToewijzingId == toewijzingId)
            bewerkToewijzingId = null;

        var snapshot = new PaneelVerwijderSnapshot(KopieerToewijzing(toewijzing), index);
        State.VerwijderToewijzing(toewijzingId);
        ResetConceptPaneel();
        Feedback.ToonInfo(
            $"Paneel {index + 1} verwijderd.",
            "Ongedaan maken",
            () => HerstelPaneelAsync(snapshot));
    }

    private Guid? VindWandId(Guid kastId)
        => State.Wanden.FirstOrDefault(wand => wand.KastIds.Contains(kastId))?.Id;

    private Guid? VindWandIdVoorToewijzing(PaneelToewijzing toewijzing)
        => toewijzing.KastIds
            .Select(VindWandId)
            .FirstOrDefault(wandId => wandId is not null);

    private Task HerstelPaneelAsync(PaneelVerwijderSnapshot snapshot)
    {
        var index = Math.Clamp(snapshot.Index, 0, State.Toewijzingen.Count);
        State.HerstelToewijzing(KopieerToewijzing(snapshot.Toewijzing), index);
        Feedback.ToonSucces($"Paneel {index + 1} is teruggezet.");
        return Task.CompletedTask;
    }

    private void RondPaneelInvoerAf()
    {
        toonKastOpdelenModal = false;
        reviewWeergaveActief = false;
        toonEditorDrawer = false;
        bewerkToewijzingId = null;
        ResetFormToewijzing();
        ResetConceptPaneel();
    }

    private PaneelToewijzing MaakPaneelToewijzing(PaneelRechthoek paneel, IReadOnlyList<Kast> dragendeKasten, Guid? toewijzingId = null) => new()
    {
        Id = toewijzingId ?? Guid.NewGuid(),
        Type = formToewijzing.Type,
        ScharnierZijde = formToewijzing.ScharnierZijde,
        PotHartVanRand = PotHartInput,
        KastIds = dragendeKasten.Select(kast => kast.Id).ToList(),
        Breedte = Math.Round(paneel.Breedte, 1),
        Hoogte = Math.Round(paneel.Hoogte, 1),
        XPositie = Math.Round(paneel.XPositie, 1),
        HoogteVanVloer = Math.Round(paneel.HoogteVanVloer, 1)
    };

    private static List<double> MaakStandaardOpdeelHoogtes(double beschikbareHoogte, int aantal)
        => PaneelConfiguratieHelper.MaakStandaardOpdeelHoogtes(beschikbareHoogte, aantal);

    private static PaneelToewijzing KopieerToewijzing(PaneelToewijzing bron)
        => IndelingFormulierHelper.KopieerToewijzing(bron);

    private sealed record PaneelVerwijderSnapshot(PaneelToewijzing Toewijzing, int Index);

    private static string VrijSegmentLabel(int index, PaneelRechthoek segment)
        => PaneelConfiguratieHelper.VrijSegmentLabel(index, segment);

    private static string TypeNaam(PaneelType type) => VisualisatieHelper.PaneelTypeLabel(type);

    private static string TypeBadgeClass(PaneelType type)
        => PaneelConfiguratieHelper.TypeBadgeClass(type);

    private static string FormatMm(double waarde) => $"{waarde:0.#} mm";
}
