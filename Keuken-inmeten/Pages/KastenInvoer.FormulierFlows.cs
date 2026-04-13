namespace Keuken_inmeten.Pages;

using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

public partial class KastenInvoer
{
    private KastFormulierModel HuidigKastFormulierModel
        => KastenInvoerFormulierFlowHelper.BouwKastFormulierModel(
            kastFormStap,
            actieveWandId,
            ActieveWandNaam(),
            formKast,
            toonTechnischeInstellingen,
            technischeControleBevestigd);

    private ApparaatFormulierModel HuidigApparaatFormulierModel
        => KastenInvoerFormulierFlowHelper.BouwApparaatFormulierModel(
            apparaatFormStap,
            ActieveWandNaam(),
            formApparaat);
}

public static class KastenInvoerFormulierFlowHelper
{
    private static readonly string[] KastFormStappen = ["Basis", "Maten", "Techniek", "Controle"];
    private static readonly string[] ApparaatFormStappen = ["Basis", "Maten", "Controle"];

    public static KastFormulierModel BouwKastFormulierModel(
        int stap,
        Guid? actieveWandId,
        string actieveWandNaam,
        Kast formKast,
        bool toonTechnischeInstellingen,
        bool technischeControleBevestigd)
    {
        var stapModel = BouwStapModel(
            stap,
            KastFormStappen,
            BepaalKastStapIntro,
            KanNaarVolgendeKastStap(stap, actieveWandId, formKast, technischeControleBevestigd));

        return new(
            Stap: stapModel,
            ActieveWandId: actieveWandId,
            ActieveWandNaam: actieveWandNaam,
            ToonTechnischeInstellingen: toonTechnischeInstellingen,
            TechnischeControleBevestigd: technischeControleBevestigd,
            TechnischeControleCheckboxLabel: BepaalTechnischeControleCheckboxLabel(formKast),
            TechnischeControleSamenvatting: BepaalTechnischeControleSamenvatting(formKast));
    }

    public static ApparaatFormulierModel BouwApparaatFormulierModel(
        int stap,
        string actieveWandNaam,
        Apparaat formApparaat)
    {
        var stapModel = BouwStapModel(
            stap,
            ApparaatFormStappen,
            BepaalApparaatStapIntro,
            KanNaarVolgendeApparaatStap(stap, formApparaat));

        return new(
            Stap: stapModel,
            ActieveWandNaam: actieveWandNaam);
    }

    private static FormulierStapModel BouwStapModel(
        int stap,
        IReadOnlyList<string> labels,
        Func<int, string> introSelector,
        bool kanNaarVolgendeStap)
    {
        if (stap < 1 || stap > labels.Count)
            throw new ArgumentOutOfRangeException(nameof(stap), stap, "Stap valt buiten de geldige wizardgrenzen.");

        return new(
            HuidigeStap: stap,
            LaatsteStap: labels.Count,
            Label: labels[stap - 1],
            Intro: introSelector(stap),
            KanNaarVolgendeStap: kanNaarVolgendeStap);
    }

    private static string BepaalKastStapIntro(int stap)
        => stap switch
        {
            1 => "Kies de wand, geef de kast een naam en start eventueel vanuit een eerder gebruikt voorbeeld.",
            2 => "Voer alleen de hoofdmaten in. Het afgeleide kasttype ziet u meteen terug.",
            3 => "Controleer de technische uitgangspunten bewust, ook als de standaardwaarden blijven staan.",
            _ => "Controleer de samenvatting en voorvertoning voordat u de kast opslaat.",
        };

    private static string BepaalApparaatStapIntro(int stap)
        => stap switch
        {
            1 => "Kies type, naam en wandcontext van het apparaat.",
            2 => "Voer alleen de maatvoering in.",
            _ => "Controleer de samenvatting en voorvertoning voordat u het apparaat opslaat.",
        };

    private static bool KanNaarVolgendeKastStap(
        int stap,
        Guid? actieveWandId,
        Kast formKast,
        bool technischeControleBevestigd)
        => stap switch
        {
            1 => actieveWandId is not null && !string.IsNullOrWhiteSpace(formKast.Naam),
            2 => formKast.Breedte > 0 && formKast.Hoogte > 0 && formKast.Diepte > 0,
            3 => technischeControleBevestigd
                && formKast.Wanddikte > 0
                && formKast.GaatjesAfstand > 0
                && formKast.EersteGaatVanBoven > 0,
            _ => false
        };

    private static bool KanNaarVolgendeApparaatStap(int stap, Apparaat formApparaat)
        => stap switch
        {
            1 => !string.IsNullOrWhiteSpace(formApparaat.Naam),
            2 => formApparaat.Breedte > 0 && formApparaat.Hoogte > 0 && formApparaat.Diepte > 0,
            _ => false
        };

    private static string BepaalTechnischeControleCheckboxLabel(Kast formKast)
        => IndelingFormulierHelper.HeeftAfwijkendeTechnischeInstellingen(formKast)
            ? "Ik heb gecontroleerd dat deze technische waarden kloppen voor deze kast."
            : "Ik heb gecontroleerd dat de standaard voor wanddikte, systeemgaten en eerste gat klopt voor deze kast.";

    private static string BepaalTechnischeControleSamenvatting(Kast formKast)
        => IndelingFormulierHelper.HeeftAfwijkendeTechnischeInstellingen(formKast)
            ? $"Gecontroleerd: {formKast.Wanddikte:0.#} mm wanddikte, {formKast.GaatjesAfstand:0.#} mm systeemgaten, {formKast.EersteGaatVanBoven:0.#} mm eerste gat"
            : $"Standaard bevestigd: {formKast.Wanddikte:0.#} mm wanddikte, {formKast.GaatjesAfstand:0.#} mm systeemgaten, {formKast.EersteGaatVanBoven:0.#} mm eerste gat";
}

public sealed record FormulierStapModel(
    int HuidigeStap,
    int LaatsteStap,
    string Label,
    string Intro,
    bool KanNaarVolgendeStap);

public sealed record KastFormulierModel(
    FormulierStapModel Stap,
    Guid? ActieveWandId,
    string ActieveWandNaam,
    bool ToonTechnischeInstellingen,
    bool TechnischeControleBevestigd,
    string TechnischeControleCheckboxLabel,
    string TechnischeControleSamenvatting);

public sealed record ApparaatFormulierModel(
    FormulierStapModel Stap,
    string ActieveWandNaam);
