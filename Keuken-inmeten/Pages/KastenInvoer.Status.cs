using Keuken_inmeten.Models;

namespace Keuken_inmeten.Pages;

public partial class KastenInvoer
{
    private void StelActieveWandContextIn(Guid? wandId)
    {
        actieveWandId = wandId;
        bewerkKastId = null;
        bewerkApparaatId = null;
        bewerkWandId = null;
        bevestigVerwijderWandId = null;
        bevestigVerwijderKastId = null;
        bevestigVerwijderApparaatId = null;
    }

    private void StelKastFormulierIn(Kast kast, bool toonFormulier, bool isBewerkmodus = false, Guid? kastId = null)
    {
        formKast = kast;
        isBewerken = isBewerkmodus;
        bewerkKastId = kastId;
        kastFormStap = 1;
        toonTechnischeInstellingen = toonFormulier && HeeftAfwijkendeTechnischeInstellingen(formKast);
        technischeControleBevestigd = false;
        toonKastFormulier = toonFormulier;
    }

    private void StelApparaatFormulierIn(
        Apparaat apparaat,
        bool toonFormulier,
        bool isBewerkmodus = false,
        Guid? apparaatId = null)
    {
        formApparaat = apparaat;
        isApparaatBewerken = isBewerkmodus;
        bewerkApparaatId = apparaatId;
        apparaatFormStap = 1;
        toonApparaatFormulier = toonFormulier;
    }
}
