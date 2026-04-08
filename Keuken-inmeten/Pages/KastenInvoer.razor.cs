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
    private bool bevestigVerwijderAlleKasten;

    private Guid? actieveWandId;
    private Kast formKast = NieuweKast();
    private bool isBewerken;
    private Guid? bewerkKastId;
    private bool toonKastFormulier;
    private bool toonTechnischeInstellingen;

    private Apparaat formApparaat = NieuwApparaat();
    private bool isApparaatBewerken;
    private Guid? bewerkApparaatId;
    private bool toonApparaatFormulier;
    private Guid? bevestigVerwijderApparaatId;

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
        if (string.IsNullOrWhiteSpace(nieuweWandNaam)) return;
        var wand = IndelingFormulierHelper.NieuweWand();
        wand.Naam = nieuweWandNaam.Trim();
        State.VoegWandToe(wand);
        nieuweWandNaam = "";
    }

    private void WandToevoegenBijEnter(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") WandToevoegen();
    }

    private void VerwijderAlleKasten()
    {
        var aantalKasten = State.Kasten.Count;
        State.VerwijderAlleKasten();
        bevestigVerwijderAlleKasten = false;
        SluitKastFormulier();
        Feedback.ToonInfo($"{aantalKasten} kast(en) verwijderd. U kunt direct een nieuwe opstelling opbouwen.");
    }

    private void WandNaamOpslaan()
    {
        if (bewerkWandId is null || string.IsNullOrWhiteSpace(bewerkWandNaam)) return;
        State.HernoemWand(bewerkWandId.Value, bewerkWandNaam);
        bewerkWandId = null;
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
        toonTechnischeInstellingen = HeeftAfwijkendeTechnischeInstellingen(formKast);
        toonKastFormulier = true;
        AutoBerekenPosities();
    }

    private void VulFormulierVanTemplate(KastTemplate template)
    {
        formKast = IndelingFormulierHelper.MaakKastVanTemplate(template);
        toonTechnischeInstellingen = HeeftAfwijkendeTechnischeInstellingen(formKast);
        AutoBerekenPosities();
    }

    private void SluitKastFormulier()
    {
        formKast = NieuweKast();
        isBewerken = false;
        bewerkKastId = null;
        toonTechnischeInstellingen = false;
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

        formKast.Type = InferKastType(formKast.Hoogte);
        AutoBerekenPosities();

        if (isBewerken && bewerkKastId.HasValue)
        {
            // Preserve planks managed via SVG drag/drop
            var bestaand = State.Kasten.Find(k => k.Id == bewerkKastId.Value);
            if (bestaand != null)
                formKast.Planken = bestaand.Planken;
            formKast.Id = bewerkKastId.Value;
            State.WerkKastBij(formKast);
        }
        else
        {
            formKast.Id = Guid.NewGuid();
            var bestaandeKasten = State.KastenVoorWand(actieveWandId.Value);
            formKast.XPositie = bestaandeKasten.Sum(k => k.Breedte);
            State.VoegKastToe(formKast, actieveWandId.Value);
        }

        formKast = NieuweKastMetVorigeWaarden();
        isBewerken = false;
        bewerkKastId = null;
        toonTechnischeInstellingen = false;
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
        toonKastFormulier = true;
        formKast = KopieerKast(kast);
        toonTechnischeInstellingen = HeeftAfwijkendeTechnischeInstellingen(formKast);
    }

    private Kast NieuweKastMetVorigeWaarden()
        => IndelingFormulierHelper.MaakKastMetVorigeWaarden(State.KastTemplates);

    private void KopieerKast(Kast kast, Guid wandId)
    {
        var kopie = KopieerKast(kast, behoudPlankIds: false);
        kopie.Id = Guid.NewGuid();
        kopie.Naam = kast.Naam + " (kopie)";
        kopie.XPositie = Math.Min(
            State.KastenVoorWand(wandId).Sum(bestaandeKast => bestaandeKast.Breedte),
            Math.Max(
                0,
                (State.Wanden.Find(w => w.KastIds.Contains(kast.Id))?.Breedte
                 ?? State.Wanden.Find(w => w.Id == wandId)?.Breedte
                 ?? double.MaxValue) - kast.Breedte));
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

    private static bool HeeftAfwijkendeTechnischeInstellingen(Kast kast)
        => IndelingFormulierHelper.HeeftAfwijkendeTechnischeInstellingen(kast);

    private void WijzigWandAfmeting(KeukenWand wand, string eigenschap, ChangeEventArgs e)
    {
        if (!double.TryParse(e.Value?.ToString(), out var waarde)) return;

        var breedte = eigenschap == nameof(KeukenWand.Breedte) ? waarde : wand.Breedte;
        var hoogte = eigenschap == nameof(KeukenWand.Hoogte) ? waarde : wand.Hoogte;
        var plintHoogte = eigenschap == nameof(KeukenWand.PlintHoogte) ? waarde : wand.PlintHoogte;
        State.WerkWandAfmetingenBij(wand.Id, breedte, hoogte, plintHoogte);
    }

    private void VerplaatsKastOpWand(KastPositieWijziging wijziging)
        => State.VerplaatsKast(wijziging.KastId, wijziging.XPositie, wijziging.HoogteVanVloer);

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
        toonApparaatFormulier = true;
    }

    private void SluitApparaatFormulier()
    {
        toonApparaatFormulier = false;
        formApparaat = NieuwApparaat();
        isApparaatBewerken = false;
        bewerkApparaatId = null;
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

        if (isApparaatBewerken && bewerkApparaatId.HasValue)
        {
            formApparaat.Id = bewerkApparaatId.Value;
            State.WerkApparaatBij(formApparaat);
        }
        else
        {
            formApparaat.Id = Guid.NewGuid();
            var wand = State.Wanden.Find(w => w.Id == actieveWandId.Value);
            var bestaandeKasten = State.KastenVoorWand(actieveWandId.Value);
            var bestaandeApparaten = State.ApparatenVoorWand(actieveWandId.Value);
            if (wand is not null)
            {
                var plaatsing = ApparaatLayoutService.BepaalStandaardPlaatsing(
                    wand,
                    formApparaat,
                    bestaandeKasten,
                    bestaandeApparaten);
                formApparaat.XPositie = plaatsing.xPositie;
                formApparaat.HoogteVanVloer = plaatsing.hoogteVanVloer;
            }
            State.VoegApparaatToe(formApparaat, actieveWandId.Value);
        }

        SluitApparaatFormulier();
    }

    private void BewerkApparaat(Apparaat apparaat, Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        isApparaatBewerken = true;
        bewerkApparaatId = apparaat.Id;
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
            Feedback.ToonFout("Kast kan niet worden teruggezet omdat de wand niet meer bestaat.");
            return Task.CompletedTask;
        }
        Feedback.ToonSucces($"Kast '{snapshot.Kast.Naam}' is teruggezet.");
        return Task.CompletedTask;
    }

    private Task HerstelApparaatAsync(ApparaatVerwijderSnapshot snapshot)
    {
        if (!State.HerstelApparaat(KopieerApparaat(snapshot.Apparaat), snapshot.WandId, snapshot.Index))
        {
            Feedback.ToonFout("Apparaat kan niet worden teruggezet omdat de wand niet meer bestaat.");
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
        => State.VerplaatsApparaat(wijziging.ApparaatId, wijziging.XPositie, wijziging.HoogteVanVloer);

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
