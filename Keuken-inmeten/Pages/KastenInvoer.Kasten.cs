using Microsoft.AspNetCore.Components.Web;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class KastenInvoer
{
    private void OpenKastFormulier(Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        StelKastFormulierIn(NieuweKastMetVorigeWaarden(), true);
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
        => StelKastFormulierIn(NieuweKast(), false);

    private static KastType InferKastType(double hoogte) => IndelingFormulierHelper.InferKastType(hoogte);

    private void KastOpslaan()
    {
        if (string.IsNullOrWhiteSpace(formKast.Naam) || actieveWandId is null)
            return;

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

        StelKastFormulierIn(NieuweKastMetVorigeWaarden(), false);
    }

    private void SelecteerKast(Guid kastId, Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        bewerkKastId = kastId;
    }

    private void BewerkKast(Kast kast, Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        StelKastFormulierIn(KopieerKast(kast), true, true, kast.Id);
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

        if (!KastPlaatsingService.TryVindVrijePlaatsing(
                wand,
                State.KastenVoorWand(actieveId),
                kast,
                out var plaatsing,
                bewerkKastId))
        {
            Feedback.ToonFout("Deze kast past niet op de gekozen wand. Kies een andere wand of maak ruimte vrij.");
            return false;
        }

        kast.XPositie = plaatsing.XPositie;
        kast.HoogteVanVloer = plaatsing.HoogteVanVloer;
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

        if (!KastPlaatsingService.TryVindVrijePlaatsing(
                wand,
                State.KastenVoorWand(wandId),
                kopie,
                out var plaatsing))
        {
            Feedback.ToonFout("Deze kast kan niet worden gekopieerd omdat er geen vrije plek meer op deze wand is.");
            return;
        }

        kopie.Id = Guid.NewGuid();
        kopie.XPositie = plaatsing.XPositie;
        kopie.HoogteVanVloer = plaatsing.HoogteVanVloer;
        State.VoegKastToe(kopie, wandId);
    }

    private void VerwijderKast(Guid id)
    {
        var snapshot = MaakKastSnapshot(id);
        if (snapshot is null)
            return;

        State.VerwijderKast(id);
        if (bewerkKastId == id)
            SluitKastFormulier();

        Feedback.ToonInfo(
            $"Kast '{snapshot.Kast.Naam}' verwijderd.",
            "Ongedaan maken",
            () => HerstelKastAsync(snapshot));
    }

    private void VerplaatsKast(Guid wandId, int vanIndex, int naarIndex)
        => State.VerplaatsKastInWand(wandId, vanIndex, naarIndex);

    private void AutoBerekenPosities()
        => formKast.MontagePlaatPosities = IndelingFormulierHelper.BerekenMontageplaatPosities(formKast);

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
        if (_clipboardKastId is null)
            return;

        var kast = State.Kasten.Find(k => k.Id == _clipboardKastId.Value);
        if (kast is null)
            return;

        KopieerKast(kast, wandId);
    }

    private static string TypeBadge(KastType type) => type switch
    {
        KastType.Onderkast => "bg-warning text-dark",
        KastType.Bovenkast => "bg-info text-dark",
        KastType.HogeKast => "bg-success",
        _ => "bg-secondary"
    };
}
