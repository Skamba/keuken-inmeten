using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class KastenInvoer
{
    private string nieuweWandNaam = "";
    private Guid? bewerkWandId;
    private string bewerkWandNaam = "";
    private Guid? bevestigVerwijderWandId;
    private Guid? bevestigVerwijderKastId;
    private Guid? _clipboardKastId;
    private bool bevestigWisAlles;
    private bool toonWandToevoegenModal;

    private Guid? actieveWandId;
    private Kast formKast = NieuweKast();
    private bool isBewerken;
    private Guid? bewerkKastId;
    private bool toonKastFormulier;
    private bool toonTechnischeInstellingen;
    private bool technischeControleBevestigd;
    private int kastFormStap = 1;

    private Apparaat formApparaat = NieuwApparaat();
    private bool isApparaatBewerken;
    private Guid? bewerkApparaatId;
    private bool toonApparaatFormulier;
    private Guid? bevestigVerwijderApparaatId;
    private int apparaatFormStap = 1;

    private const int LaatsteKastFormStap = 4;
    private const int LaatsteApparaatFormStap = 3;

    private static readonly string[] KastFormStappen = ["Basis", "Maten", "Techniek", "Controle"];
    private static readonly string[] ApparaatFormStappen = ["Basis", "Maten", "Controle"];

    protected override void OnInitialized()
        => State.OnStateChanged += HandleStateChanged;

    public void Dispose()
        => State.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged()
    {
        if (actieveWandId.HasValue && !State.Wanden.Exists(wand => wand.Id == actieveWandId.Value))
        {
            actieveWandId = null;
            bewerkKastId = null;
            bewerkApparaatId = null;
            bewerkWandId = null;
            bevestigVerwijderWandId = null;
            bevestigVerwijderKastId = null;
            bevestigVerwijderApparaatId = null;
        }

        _ = InvokeAsync(StateHasChanged);
    }

    private static Kast NieuweKast() => IndelingFormulierHelper.NieuweKast();

    private static Apparaat NieuwApparaat(ApparaatType type = ApparaatType.Oven)
        => IndelingFormulierHelper.NieuwApparaat(type);

    private void WandToevoegen()
    {
        var wandNaam = nieuweWandNaam.Trim();
        if (string.IsNullOrWhiteSpace(wandNaam)) return;

        var wand = IndelingFormulierHelper.NieuweWand();
        wand.Naam = wandNaam;
        State.VoegWandToe(wand);
        SluitWandToevoegenModal();
    }

    private void WandToevoegenBijEnter(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") WandToevoegen();
    }

    private void OpenWandToevoegenModal()
    {
        nieuweWandNaam = "";
        toonWandToevoegenModal = true;
    }

    private void SluitWandToevoegenModal()
    {
        toonWandToevoegenModal = false;
        nieuweWandNaam = "";
    }

    private async Task WisAllesAsync()
    {
        bevestigWisAlles = false;
        SluitKastFormulier();
        SluitApparaatFormulier();
        SluitWandWerkruimte();
        await Projectbeheer.WisAllesAsync();
    }

    private void WandNaamOpslaan()
    {
        if (bewerkWandId is null || string.IsNullOrWhiteSpace(bewerkWandNaam)) return;
        State.HernoemWand(bewerkWandId.Value, bewerkWandNaam);
        bewerkWandId = null;
    }

    private void SluitGatenOpActieveWand()
    {
        if (actieveWandId is Guid wandId)
            State.SluitAlleGatenOpWand(wandId);
    }

    private void OpenWandWerkruimte(Guid wandId)
    {
        actieveWandId = wandId;
        bewerkKastId = null;
        bewerkApparaatId = null;
        bewerkWandId = null;
        bevestigVerwijderWandId = null;
        bevestigVerwijderKastId = null;
        bevestigVerwijderApparaatId = null;
    }

    private void SluitWandWerkruimte()
    {
        actieveWandId = null;
        bewerkKastId = null;
        bewerkApparaatId = null;
        bewerkWandId = null;
        bevestigVerwijderWandId = null;
        bevestigVerwijderKastId = null;
        bevestigVerwijderApparaatId = null;
    }

    private void OpenKastFormulier(Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        formKast = NieuweKastMetVorigeWaarden();
        isBewerken = false;
        bewerkKastId = null;
        kastFormStap = 1;
        toonTechnischeInstellingen = HeeftAfwijkendeTechnischeInstellingen(formKast);
        technischeControleBevestigd = false;
        toonKastFormulier = true;
        AutoBerekenPosities();
    }

    private void VulFormulierVanTemplate(KastTemplate template)
    {
        formKast = IndelingFormulierHelper.MaakKastVanTemplate(template);
        toonTechnischeInstellingen = HeeftAfwijkendeTechnischeInstellingen(formKast);
        technischeControleBevestigd = false;
        AutoBerekenPosities();
    }

    private void SluitKastFormulier()
    {
        formKast = NieuweKast();
        isBewerken = false;
        bewerkKastId = null;
        kastFormStap = 1;
        toonTechnischeInstellingen = false;
        technischeControleBevestigd = false;
        toonKastFormulier = false;
    }

    private void OnWandGewijzigd(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out var id))
            actieveWandId = id;
    }

    private static KastType InferKastType(double hoogte) => IndelingFormulierHelper.InferKastType(hoogte);

    private void KastOpslaan()
    {
        if (string.IsNullOrWhiteSpace(formKast.Naam)) return;
        if (actieveWandId is null) return;
        if (!TryMaakOpTeSlaanKast(out var opTeSlaanKast, out var doelWandId))
            return;

        if (isBewerken && bewerkKastId.HasValue)
        {
            opTeSlaanKast.Id = bewerkKastId.Value;
            if (!State.WerkKastBijOpWand(opTeSlaanKast, doelWandId))
                return;
        }
        else
        {
            opTeSlaanKast.Id = Guid.NewGuid();
            if (!State.VoegKastToe(opTeSlaanKast, doelWandId))
                return;
        }

        formKast = NieuweKastMetVorigeWaarden();
        isBewerken = false;
        bewerkKastId = null;
        kastFormStap = 1;
        toonTechnischeInstellingen = false;
        technischeControleBevestigd = false;
        toonKastFormulier = false;
    }

    private void SelecteerKast(Guid kastId, Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        bewerkKastId = kastId;
        // Only highlight — edit form opens via the ✎ button
    }

    private void BewerkKast(Kast kast, Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        isBewerken = true;
        bewerkKastId = kast.Id;
        kastFormStap = 1;
        toonKastFormulier = true;
        formKast = KopieerKast(kast);
        toonTechnischeInstellingen = HeeftAfwijkendeTechnischeInstellingen(formKast);
        technischeControleBevestigd = false;
    }

    private Kast NieuweKastMetVorigeWaarden()
        => IndelingFormulierHelper.MaakKastMetVorigeWaarden(State.KastTemplates);

    private bool TryMaakOpTeSlaanKast(out Kast kast, out Guid wandId)
    {
        kast = NieuweKast();
        wandId = Guid.Empty;

        if (actieveWandId is not Guid actieveId)
            return false;

        var wand = State.Wanden.Find(item => item.Id == actieveId);
        if (wand is null)
            return false;

        kast = KopieerKast(formKast);
        kast.Type = InferKastType(kast.Hoogte);
        kast.MontagePlaatPosities = IndelingFormulierHelper.BerekenMontageplaatPosities(kast);

        if (isBewerken && bewerkKastId is Guid bestaandKastId)
        {
            var bestaand = State.Kasten.Find(item => item.Id == bestaandKastId);
            if (bestaand is null)
                return false;

            kast.Id = bestaand.Id;
            kast.Planken = KopieerKast(bestaand).Planken;
        }

        if (!IndelingFormulierHelper.TryVindVrijeKastPlaatsing(
                wand,
                State.KastenVoorWand(actieveId),
                kast,
                out var plaatsing,
                bewerkKastId))
        {
            Feedback.ToonFout("Deze kast past niet op de gekozen wand. Kies een andere wand of maak ruimte vrij.");
            return false;
        }

        kast.XPositie = plaatsing.xPositie;
        kast.HoogteVanVloer = plaatsing.hoogteVanVloer;
        wandId = actieveId;
        return true;
    }

    private void KopieerKast(Kast kast, Guid wandId)
    {
        var kopie = KopieerKast(kast, behoudPlankIds: false);
        kopie.Naam = kast.Naam + " (kopie)";

        var wand = State.Wanden.Find(item => item.Id == wandId);
        if (wand is null)
            return;

        if (!IndelingFormulierHelper.TryVindVrijeKastPlaatsing(
                wand,
                State.KastenVoorWand(wandId),
                kopie,
                out var plaatsing))
        {
            Feedback.ToonFout("Deze kast kan niet worden gekopieerd omdat er geen vrije plek meer op deze wand is.");
            return;
        }

        kopie.Id = Guid.NewGuid();
        kopie.XPositie = plaatsing.xPositie;
        kopie.HoogteVanVloer = plaatsing.hoogteVanVloer;
        State.VoegKastToe(kopie, wandId);
    }

    private void VerwijderKast(Guid id)
    {
        var snapshot = MaakKastSnapshot(id);
        if (snapshot is null)
            return;

        State.VerwijderKast(id);
        if (bewerkKastId == id) SluitKastFormulier();
        Feedback.ToonInfo(
            $"Kast '{snapshot.Kast.Naam}' verwijderd.",
            "Ongedaan maken",
            () => HerstelKastAsync(snapshot));
    }

    private void VerplaatsKast(Guid wandId, int vanIndex, int naarIndex)
    {
        State.VerplaatsKastInWand(wandId, vanIndex, naarIndex);
    }

    private void AutoBerekenPosities()
        => formKast.MontagePlaatPosities = IndelingFormulierHelper.BerekenMontageplaatPosities(formKast);

    private void ToggleTechnischeInstellingen()
        => toonTechnischeInstellingen = !toonTechnischeInstellingen;

    private void MarkeerTechnischeControleOnbevestigd()
        => technischeControleBevestigd = false;

    private static bool HeeftAfwijkendeTechnischeInstellingen(Kast kast)
        => IndelingFormulierHelper.HeeftAfwijkendeTechnischeInstellingen(kast);

    private static string KastFormStapLabel(int stap)
        => KastFormStappen[stap - 1];

    private static string KastFormStapIntro(int stap)
        => stap switch
        {
            1 => "Kies de wand, geef de kast een naam en start eventueel vanuit een eerder gebruikt voorbeeld.",
            2 => "Voer alleen de hoofdmaten in. Het afgeleide kasttype ziet u meteen terug.",
            3 => "Controleer de technische uitgangspunten bewust, ook als de standaardwaarden blijven staan.",
            _ => "Controleer de samenvatting en voorvertoning voordat u de kast opslaat.",
        };

    private static string ApparaatFormStapLabel(int stap)
        => ApparaatFormStappen[stap - 1];

    private static string ApparaatFormStapIntro(int stap)
        => stap switch
        {
            1 => "Kies type, naam en wandcontext van het apparaat.",
            2 => "Voer alleen de maatvoering in.",
            _ => "Controleer de samenvatting en voorvertoning voordat u het apparaat opslaat.",
        };

    private string ActieveWandNaam()
        => actieveWandId is Guid id
            ? State.Wanden.Find(wand => wand.Id == id)?.Naam ?? "Onbekende wand"
            : "Geen wand gekozen";

    private bool KanNaarVolgendeKastStap()
        => kastFormStap switch
        {
            1 => actieveWandId is not null && !string.IsNullOrWhiteSpace(formKast.Naam),
            2 => formKast.Breedte > 0 && formKast.Hoogte > 0 && formKast.Diepte > 0,
            3 => technischeControleBevestigd
                && formKast.Wanddikte > 0
                && formKast.GaatjesAfstand > 0
                && formKast.EersteGaatVanBoven > 0,
            _ => false
        };

    private string TechnischeControleCheckboxLabel()
        => HeeftAfwijkendeTechnischeInstellingen(formKast)
            ? "Ik heb gecontroleerd dat deze technische waarden kloppen voor deze kast."
            : "Ik heb gecontroleerd dat de standaard voor wanddikte, systeemgaten en eerste gat klopt voor deze kast.";

    private string TechnischeControleSamenvatting()
        => HeeftAfwijkendeTechnischeInstellingen(formKast)
            ? $"Gecontroleerd: {formKast.Wanddikte:0.#} mm wanddikte, {formKast.GaatjesAfstand:0.#} mm systeemgaten, {formKast.EersteGaatVanBoven:0.#} mm eerste gat"
            : $"Standaard bevestigd: {formKast.Wanddikte:0.#} mm wanddikte, {formKast.GaatjesAfstand:0.#} mm systeemgaten, {formKast.EersteGaatVanBoven:0.#} mm eerste gat";

    private void VolgendeKastFormStap()
    {
        if (kastFormStap >= LaatsteKastFormStap || !KanNaarVolgendeKastStap())
            return;

        kastFormStap++;
    }

    private void VorigeKastFormStap()
    {
        if (kastFormStap <= 1)
            return;

        kastFormStap--;
    }

    private void WijzigWandAfmeting(KeukenWand wand, string eigenschap, ChangeEventArgs e)
    {
        if (!double.TryParse(e.Value?.ToString(), out var waarde)) return;

        var breedte = eigenschap == nameof(KeukenWand.Breedte) ? waarde : wand.Breedte;
        var hoogte = eigenschap == nameof(KeukenWand.Hoogte) ? waarde : wand.Hoogte;
        var plintHoogte = eigenschap == nameof(KeukenWand.PlintHoogte) ? waarde : wand.PlintHoogte;
        if (Math.Abs(wand.Breedte - breedte) < 0.001
            && Math.Abs(wand.Hoogte - hoogte) < 0.001
            && Math.Abs(wand.PlintHoogte - plintHoogte) < 0.001)
        {
            return;
        }

        if (!State.WerkWandAfmetingenBij(wand.Id, breedte, hoogte, plintHoogte))
        {
            Feedback.ToonFout("Deze wandmaat past niet meer bij de huidige kasten en apparaten. Maak eerst ruimte vrij of verplaats de inhoud.");
        }
    }

    private void VerplaatsKastOpWand(KastPositieWijziging wijziging)
    {
        var kast = State.Kasten.Find(item => item.Id == wijziging.KastId);
        if (kast is null)
            return;

        if (Math.Abs(kast.XPositie - wijziging.XPositie) < 0.001
            && Math.Abs(kast.HoogteVanVloer - wijziging.HoogteVanVloer) < 0.001)
        {
            return;
        }

        if (!State.VerplaatsKast(wijziging.KastId, wijziging.XPositie, wijziging.HoogteVanVloer))
            Feedback.ToonFout("Deze kast kan hier niet staan. Houd de kast binnen de wand en vrij van andere kasten.");
    }

    private void VerwerkPlankActie(WandPlankActie actie)
    {
        switch (actie.Type)
        {
            case WandPlankActieType.Toevoegen:
                State.VoegPlankToe(actie.KastId, actie.HoogteVanBodem, actie.PlankId);
                break;
            case WandPlankActieType.Verplaatsen:
                State.VerplaatsPlank(actie.KastId, actie.PlankId, actie.HoogteVanBodem);
                break;
            case WandPlankActieType.Verwijderen:
                State.VerwijderPlank(actie.KastId, actie.PlankId);
                break;
            case WandPlankActieType.Herstellen:
                State.HerstelPlank(
                    actie.KastId,
                    new Plank
                    {
                        Id = actie.PlankId,
                        HoogteVanBodem = actie.HoogteVanBodem
                    },
                    actie.Index);
                break;
        }
    }

    private void KopieerNaarKlembord(Guid kastId) => _clipboardKastId = kastId;

    private void PlakUitKlembord(Guid wandId)
    {
        if (_clipboardKastId is null) return;
        var kast = State.Kasten.Find(k => k.Id == _clipboardKastId.Value);
        if (kast is null) return;
        KopieerKast(kast, wandId);
    }

    // --- Apparaten ---

    private void OpenApparaatFormulier(Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        formApparaat = NieuwApparaat();
        isApparaatBewerken = false;
        bewerkApparaatId = null;
        apparaatFormStap = 1;
        toonApparaatFormulier = true;
    }

    private void SluitApparaatFormulier()
    {
        toonApparaatFormulier = false;
        formApparaat = NieuwApparaat();
        isApparaatBewerken = false;
        bewerkApparaatId = null;
        apparaatFormStap = 1;
    }

    private bool KanNaarVolgendeApparaatStap()
        => apparaatFormStap switch
        {
            1 => !string.IsNullOrWhiteSpace(formApparaat.Naam),
            2 => formApparaat.Breedte > 0 && formApparaat.Hoogte > 0 && formApparaat.Diepte > 0,
            _ => false
        };

    private void VolgendeApparaatFormStap()
    {
        if (apparaatFormStap >= LaatsteApparaatFormStap || !KanNaarVolgendeApparaatStap())
            return;

        apparaatFormStap++;
    }

    private void VorigeApparaatFormStap()
    {
        if (apparaatFormStap <= 1)
            return;

        apparaatFormStap--;
    }

    private void OnApparaatTypeGewijzigd(ChangeEventArgs e)
    {
        if (Enum.TryParse<ApparaatType>(e.Value?.ToString(), out var type))
        {
            formApparaat.Type = type;
            if (!isApparaatBewerken)
            {
                var standaardApparaat = NieuwApparaat(type);
                formApparaat.Breedte = standaardApparaat.Breedte;
                formApparaat.Hoogte = standaardApparaat.Hoogte;
                formApparaat.Diepte = standaardApparaat.Diepte;
            }
        }
    }

    private void ApparaatOpslaan()
    {
        if (string.IsNullOrWhiteSpace(formApparaat.Naam)) return;
        if (actieveWandId is null) return;

        var opTeSlaanApparaat = KopieerApparaat(formApparaat);

        if (isApparaatBewerken && bewerkApparaatId.HasValue)
        {
            opTeSlaanApparaat.Id = bewerkApparaatId.Value;
            if (!State.WerkApparaatBij(opTeSlaanApparaat))
            {
                Feedback.ToonFout("Dit apparaat past niet meer op deze plek. Verklein het apparaat of maak eerst ruimte vrij.");
                return;
            }
        }
        else
        {
            var wand = State.Wanden.Find(w => w.Id == actieveWandId.Value);
            var bestaandeKasten = State.KastenVoorWand(actieveWandId.Value);
            var bestaandeApparaten = State.ApparatenVoorWand(actieveWandId.Value);
            if (wand is null)
                return;

            if (!ApparaatLayoutService.TryBepaalStandaardPlaatsing(
                    wand,
                    opTeSlaanApparaat,
                    bestaandeKasten,
                    bestaandeApparaten,
                    out var plaatsing))
            {
                Feedback.ToonFout("Dit apparaat past niet meer op de gekozen wand. Maak ruimte vrij of kies een andere oplossing.");
                return;
            }

            opTeSlaanApparaat.Id = Guid.NewGuid();
            opTeSlaanApparaat.XPositie = plaatsing.xPositie;
            opTeSlaanApparaat.HoogteVanVloer = plaatsing.hoogteVanVloer;

            if (!State.VoegApparaatToe(opTeSlaanApparaat, actieveWandId.Value))
                return;
        }

        SluitApparaatFormulier();
    }

    private void BewerkApparaat(Apparaat apparaat, Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        isApparaatBewerken = true;
        bewerkApparaatId = apparaat.Id;
        apparaatFormStap = 1;
        toonApparaatFormulier = true;
        formApparaat = KopieerApparaat(apparaat);
    }

    private void VerwijderApparaat(Guid id)
    {
        var snapshot = MaakApparaatSnapshot(id);
        if (snapshot is null)
            return;

        State.VerwijderApparaat(id);
        if (bewerkApparaatId == id) SluitApparaatFormulier();
        Feedback.ToonInfo(
            $"Apparaat '{snapshot.Apparaat.Naam}' verwijderd.",
            "Ongedaan maken",
            () => HerstelApparaatAsync(snapshot));
    }

    private void BevestigVerwijderWand(Guid wandId)
    {
        var wand = State.Wanden.Find(item => item.Id == wandId);
        if (wand is null)
            return;

        var wandNaam = wand.Naam;
        State.VerwijderWand(wandId);
        bevestigVerwijderWandId = null;
        bewerkWandId = null;

        if (actieveWandId == wandId)
        {
            SluitKastFormulier();
            SluitApparaatFormulier();
            actieveWandId = null;
        }

        Feedback.ToonInfo($"Wand '{wandNaam}' verwijderd.");
    }

    private KastVerwijderSnapshot? MaakKastSnapshot(Guid id)
    {
        var kast = State.Kasten.Find(item => item.Id == id);
        var wand = State.WandVoorKast(id);
        if (kast is null || wand is null)
            return null;

        var gekoppeldeToewijzingen = State.Toewijzingen
            .Select((toewijzing, index) => new ToewijzingSnapshot(KopieerToewijzing(toewijzing), index))
            .Where(snapshot => snapshot.Toewijzing.KastIds.Contains(id))
            .ToList();

        return new KastVerwijderSnapshot(
            KopieerKast(kast),
            wand.Id,
            wand.KastIds.IndexOf(id),
            gekoppeldeToewijzingen);
    }

    private ApparaatVerwijderSnapshot? MaakApparaatSnapshot(Guid id)
    {
        var apparaat = State.Apparaten.Find(item => item.Id == id);
        var wand = State.Wanden.Find(item => item.ApparaatIds.Contains(id));
        if (apparaat is null || wand is null)
            return null;

        return new ApparaatVerwijderSnapshot(
            KopieerApparaat(apparaat),
            wand.Id,
            wand.ApparaatIds.IndexOf(id));
    }

    private Task HerstelKastAsync(KastVerwijderSnapshot snapshot)
    {
        var hersteld = State.HerstelKastMetToewijzingen(
            KopieerKast(snapshot.Kast),
            snapshot.WandId,
            snapshot.KastIndex,
            [.. snapshot.Toewijzingen.Select(item => new GeindexeerdeToewijzing(KopieerToewijzing(item.Toewijzing), item.Index))]);
        if (!hersteld)
        {
            Feedback.ToonFout("Kast kan niet worden teruggezet omdat de wand ontbreekt of de oude plek niet meer vrij is.");
            return Task.CompletedTask;
        }
        Feedback.ToonSucces($"Kast '{snapshot.Kast.Naam}' is teruggezet.");
        return Task.CompletedTask;
    }

    private Task HerstelApparaatAsync(ApparaatVerwijderSnapshot snapshot)
    {
        if (!State.HerstelApparaat(KopieerApparaat(snapshot.Apparaat), snapshot.WandId, snapshot.Index))
        {
            Feedback.ToonFout("Apparaat kan niet worden teruggezet omdat de wand ontbreekt of de oude plek niet meer vrij is.");
            return Task.CompletedTask;
        }
        Feedback.ToonSucces($"Apparaat '{snapshot.Apparaat.Naam}' is teruggezet.");
        return Task.CompletedTask;
    }

    private static Kast KopieerKast(Kast bron, bool behoudPlankIds = true)
        => IndelingFormulierHelper.KopieerKast(bron, behoudPlankIds);

    private static Apparaat KopieerApparaat(Apparaat bron)
        => IndelingFormulierHelper.KopieerApparaat(bron);

    private static PaneelToewijzing KopieerToewijzing(PaneelToewijzing bron)
        => IndelingFormulierHelper.KopieerToewijzing(bron);

    private sealed record ToewijzingSnapshot(PaneelToewijzing Toewijzing, int Index);

    private sealed record KastVerwijderSnapshot(
        Kast Kast,
        Guid WandId,
        int KastIndex,
        List<ToewijzingSnapshot> Toewijzingen);

    private sealed record ApparaatVerwijderSnapshot(Apparaat Apparaat, Guid WandId, int Index);

    private void VerplaatsApparaatOpWand(ApparaatPositieWijziging wijziging)
    {
        var apparaat = State.Apparaten.Find(item => item.Id == wijziging.ApparaatId);
        if (apparaat is null)
            return;

        if (Math.Abs(apparaat.XPositie - wijziging.XPositie) < 0.001
            && Math.Abs(apparaat.HoogteVanVloer - wijziging.HoogteVanVloer) < 0.001)
        {
            return;
        }

        if (!State.VerplaatsApparaat(wijziging.ApparaatId, wijziging.XPositie, wijziging.HoogteVanVloer))
            Feedback.ToonFout("Dit apparaat kan hier niet staan. Houd het vrij van kasten, andere apparaten en buitenranden van de wand.");
    }

    private static string ApparaatTypeLabel(ApparaatType type)
        => IndelingFormulierHelper.ApparaatTypeLabel(type);

    private static string ApparaatIcoon(ApparaatType type) => type switch
    {
        ApparaatType.Oven => "🔥",
        ApparaatType.Magnetron => "📡",
        ApparaatType.Vaatwasser => "🫧",
        ApparaatType.Koelkast => "❄️",
        ApparaatType.Vriezer => "🧊",
        ApparaatType.Kookplaat => "♨️",
        ApparaatType.Afzuigkap => "💨",
        _ => "📦"
    };

    private static string TypeBadge(KastType type) => type switch
    {
        KastType.Onderkast => "bg-warning text-dark",
        KastType.Bovenkast => "bg-info text-dark",
        KastType.HogeKast => "bg-success",
        _ => "bg-secondary"
    };
}
