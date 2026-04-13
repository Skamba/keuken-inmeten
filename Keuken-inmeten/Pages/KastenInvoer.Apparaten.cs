using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class KastenInvoer
{
    private void OpenApparaatFormulier(Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        StelApparaatFormulierIn(NieuwApparaat(), true);
    }

    private void SluitApparaatFormulier()
        => StelApparaatFormulierIn(NieuwApparaat(), false);

    private static string ApparaatFormStapLabel(int stap)
        => ApparaatFormStappen[stap - 1];

    private static string ApparaatFormStapIntro(int stap)
        => stap switch
        {
            1 => "Kies type, naam en wandcontext van het apparaat.",
            2 => "Voer alleen de maatvoering in.",
            _ => "Controleer de samenvatting en voorvertoning voordat u het apparaat opslaat.",
        };

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
        if (string.IsNullOrWhiteSpace(formApparaat.Naam) || actieveWandId is null)
            return;

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

        StelApparaatFormulierIn(NieuwApparaat(), false);
    }

    private void BewerkApparaat(Apparaat apparaat, Guid wandId)
    {
        OpenWandWerkruimte(wandId);
        StelApparaatFormulierIn(KopieerApparaat(apparaat), true, true, apparaat.Id);
    }

    private void VerwijderApparaat(Guid id)
    {
        var snapshot = MaakApparaatSnapshot(id);
        if (snapshot is null)
            return;

        State.VerwijderApparaat(id);
        if (bewerkApparaatId == id)
            SluitApparaatFormulier();

        Feedback.ToonInfo(
            $"Apparaat '{snapshot.Apparaat.Naam}' verwijderd.",
            "Ongedaan maken",
            () => HerstelApparaatAsync(snapshot));
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

    private static Apparaat KopieerApparaat(Apparaat bron)
        => IndelingFormulierHelper.KopieerApparaat(bron);

    private static string ApparaatTypeLabel(ApparaatType type)
        => IndelingFormulierHelper.ApparaatTypeLabel(type);

}
