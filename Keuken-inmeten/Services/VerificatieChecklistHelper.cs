namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class VerificatieChecklistHelper
{
    public static VerificatieChecklistStatus BouwStatus(PaneelResultaat resultaat, PaneelVerificatieStatus checks)
    {
        var heeftScharnierCheck = resultaat.Type == PaneelType.Deur && resultaat.Boorgaten.Count > 0;
        var aantalAfgevinkt = (checks.MatenOk ? 1 : 0) + (heeftScharnierCheck && checks.ScharnierPositiesOk ? 1 : 0);
        var totaalChecks = heeftScharnierCheck ? 2 : 1;
        var openChecks = totaalChecks - aantalAfgevinkt;
        var volgendeControle = !checks.MatenOk
            ? "Meet de maat in de opening na"
            : heeftScharnierCheck && !checks.ScharnierPositiesOk
                ? "Controleer de systeemgaten"
                : "Alle controles voor dit paneel zijn klaar";

        var huidigeControleHint = volgendeControle switch
        {
            "Meet de maat in de opening na" => "Vergelijk de echte opening met de maat hieronder voordat u verdergaat.",
            "Controleer de systeemgaten" => "Bevestig nu of de scharnierplaatposities in de gaatjesrij kloppen.",
            _ => "Alle checkliststappen voor dit paneel zijn afgevinkt."
        };

        return new VerificatieChecklistStatus(
            checks,
            heeftScharnierCheck,
            aantalAfgevinkt == totaalChecks,
            aantalAfgevinkt,
            totaalChecks,
            openChecks,
            volgendeControle,
            huidigeControleHint,
            openChecks == 0 ? "Alles klaar" : $"{openChecks} open",
            aantalAfgevinkt == totaalChecks ? "Klaar" : $"{aantalAfgevinkt}/{totaalChecks} controles");
    }
}

public sealed record VerificatieChecklistStatus(
    PaneelVerificatieStatus Checks,
    bool HeeftScharnierCheck,
    bool Geverifieerd,
    int AantalAfgevinkt,
    int TotaalChecks,
    int OpenChecks,
    string VolgendeControle,
    string HuidigeControleHint,
    string OpenChecksLabel,
    string PaneelTabStatusTekst);
