using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public enum VerificatieFase
{
    Overzicht,
    PaneelVerificatie,
    Afronding
}

public partial class Verificatie
{
    private VerificatiePaginaModel HuidigePaginaModel
    {
        get
        {
            var flowStatus = StappenFlowHelper.BepaalStatus(State);
            var paneelBronnen = State.BerekenResultaten()
                .Select(MaakPaneelBron)
                .ToList();

            return VerificatieReadModelHelper.BouwPaginaModel(
                routeGate: StappenFlowHelper.BepaalRouteGate("verificatie", flowStatus),
                wanden: State.Wanden,
                paneelBronnen: paneelBronnen,
                fase: _fase,
                paneelIndex: _paneelIndex,
                laatsteGebruiktePotHartVanRand: State.LaatstGebruiktePotHartVanRand);
        }
    }

    private VerificatiePaneelBron MaakPaneelBron(PaneelResultaat resultaat)
    {
        var checklist = VerificatieChecklistHelper.BouwStatus(resultaat, State.LeesVerificatieStatus(resultaat.ToewijzingId));
        var toewijzing = State.Toewijzingen.FirstOrDefault(item => item.Id == resultaat.ToewijzingId);
        var wandId = resultaat.KastIds
            .Select(State.WandVoorKast)
            .FirstOrDefault(kandidaat => kandidaat is not null)?.Id;

        return new(
            Resultaat: resultaat,
            Checklist: checklist,
            Toewijzing: toewijzing,
            PaneelKasten: State.ZoekKasten(resultaat.KastIds),
            WandId: wandId,
            WandNaam: State.WandNaamVoorKasten(resultaat.KastIds));
    }
}

public static class VerificatieReadModelHelper
{
    public static VerificatiePaginaModel BouwPaginaModel(
        StapRouteGate? routeGate,
        IReadOnlyList<KeukenWand> wanden,
        IReadOnlyList<VerificatiePaneelBron> paneelBronnen,
        VerificatieFase fase,
        int paneelIndex,
        double laatsteGebruiktePotHartVanRand)
    {
        var resultaten = paneelBronnen
            .Select(item => item.Resultaat)
            .ToList();
        var checklists = paneelBronnen
            .Select(item => item.Checklist)
            .ToList();

        return new(
            RouteGate: routeGate,
            Resultaten: resultaten,
            Checklists: checklists,
            Overzicht: BouwOverzichtModel(wanden, paneelBronnen),
            ActiefPaneel: BouwActiefPaneelModel(paneelBronnen, fase, paneelIndex, laatsteGebruiktePotHartVanRand),
            Afronding: BouwAfrondingModel(paneelBronnen, fase));
    }

    private static VerificatieOverzichtModel? BouwOverzichtModel(
        IReadOnlyList<KeukenWand> wanden,
        IReadOnlyList<VerificatiePaneelBron> paneelBronnen)
    {
        if (paneelBronnen.Count == 0)
            return null;

        var taken = paneelBronnen
            .Select(MaakPaneelTaakModel)
            .ToList();
        var taakGroepen = wanden
            .Select(wand =>
            {
                var wandTaken = taken
                    .Where(taak => taak.WandId == wand.Id)
                    .ToList();

                return wandTaken.Count == 0
                    ? null
                    : new VerificatieTaakGroepModel(wand.Id, wand.Naam, wandTaken);
            })
            .Where(groep => groep is not null)
            .Cast<VerificatieTaakGroepModel>()
            .ToList();
        var aantalAfgerond = taken.Count(taak => taak.Geverifieerd);
        var eersteOngecontroleerdIndex = taken.FirstOrDefault(taak => !taak.Geverifieerd)?.Index ?? 0;
        var samenvattingTaak = aantalAfgerond == taken.Count
            ? taken[^1]
            : taken.First(taak => !taak.Geverifieerd);

        return new(
            TaakGroepen: taakGroepen,
            AantalAfgerond: aantalAfgerond,
            EersteOngecontroleerdIndex: eersteOngecontroleerdIndex,
            TotaalOpenChecks: taken.Sum(taak => taak.OpenChecks),
            AlGestart: aantalAfgerond > 0,
            SamenvattingTaak: samenvattingTaak);
    }

    private static VerificatiePaneelTaakModel MaakPaneelTaakModel(VerificatiePaneelBron paneelBron, int index)
        => new(
            Index: index,
            Resultaat: paneelBron.Resultaat,
            WandId: paneelBron.WandId,
            WandNaam: paneelBron.WandNaam,
            Geverifieerd: paneelBron.Checklist.Geverifieerd,
            OpenChecks: paneelBron.Checklist.OpenChecks,
            VolgendeControle: paneelBron.Checklist.VolgendeControle,
            OpeningB: LeesOpeningBreedte(paneelBron.Resultaat),
            OpeningH: LeesOpeningHoogte(paneelBron.Resultaat));

    private static VerificatieActiefPaneelModel? BouwActiefPaneelModel(
        IReadOnlyList<VerificatiePaneelBron> paneelBronnen,
        VerificatieFase fase,
        int paneelIndex,
        double laatsteGebruiktePotHartVanRand)
    {
        if (fase != VerificatieFase.PaneelVerificatie || paneelIndex >= paneelBronnen.Count)
            return null;

        var paneelBron = paneelBronnen[paneelIndex];

        return new(
            Resultaat: paneelBron.Resultaat,
            Toewijzing: paneelBron.Toewijzing,
            PaneelKasten: paneelBron.PaneelKasten,
            Checklist: paneelBron.Checklist,
            OpeningB: LeesOpeningBreedte(paneelBron.Resultaat),
            OpeningH: LeesOpeningHoogte(paneelBron.Resultaat),
            PotHartVanRand: LeesPotHartVanRand(paneelBron, laatsteGebruiktePotHartVanRand),
            AfgerondePanelen: paneelBronnen.Count(item => item.Checklist.Geverifieerd),
            OpenPaneelControles: paneelBronnen.Sum(item => item.Checklist.OpenChecks),
            WandNaam: paneelBron.WandNaam,
            TotaalPanelen: paneelBronnen.Count);
    }

    private static VerificatieAfrondingModel? BouwAfrondingModel(
        IReadOnlyList<VerificatiePaneelBron> paneelBronnen,
        VerificatieFase fase)
    {
        if (fase != VerificatieFase.Afronding)
            return null;

        return new(
            AantalGeverifieerd: paneelBronnen.Count(item => item.Checklist.Geverifieerd),
            TotaalPanelen: paneelBronnen.Count);
    }

    private static double LeesOpeningBreedte(PaneelResultaat resultaat)
        => resultaat.MaatInfo?.OpeningsRechthoek.Breedte ?? resultaat.Breedte;

    private static double LeesOpeningHoogte(PaneelResultaat resultaat)
        => resultaat.MaatInfo?.OpeningsRechthoek.Hoogte ?? resultaat.Hoogte;

    private static double LeesPotHartVanRand(
        VerificatiePaneelBron paneelBron,
        double laatsteGebruiktePotHartVanRand)
        => paneelBron.Resultaat.Boorgaten.FirstOrDefault()?.X
            ?? paneelBron.Toewijzing?.PotHartVanRand
            ?? laatsteGebruiktePotHartVanRand;
}

public sealed record VerificatiePaneelBron(
    PaneelResultaat Resultaat,
    VerificatieChecklistStatus Checklist,
    PaneelToewijzing? Toewijzing,
    IReadOnlyList<Kast> PaneelKasten,
    Guid? WandId,
    string WandNaam);

public sealed record VerificatiePaginaModel(
    StapRouteGate? RouteGate,
    IReadOnlyList<PaneelResultaat> Resultaten,
    IReadOnlyList<VerificatieChecklistStatus> Checklists,
    VerificatieOverzichtModel? Overzicht,
    VerificatieActiefPaneelModel? ActiefPaneel,
    VerificatieAfrondingModel? Afronding);

public sealed record VerificatieOverzichtModel(
    IReadOnlyList<VerificatieTaakGroepModel> TaakGroepen,
    int AantalAfgerond,
    int EersteOngecontroleerdIndex,
    int TotaalOpenChecks,
    bool AlGestart,
    VerificatiePaneelTaakModel SamenvattingTaak);

public sealed record VerificatieTaakGroepModel(
    Guid WandId,
    string WandNaam,
    IReadOnlyList<VerificatiePaneelTaakModel> Taken)
{
    public int OpenTaken => Taken.Count(taak => !taak.Geverifieerd);
    public int AfgerondeTaken => Taken.Count(taak => taak.Geverifieerd);
}

public sealed record VerificatiePaneelTaakModel(
    int Index,
    PaneelResultaat Resultaat,
    Guid? WandId,
    string WandNaam,
    bool Geverifieerd,
    int OpenChecks,
    string VolgendeControle,
    double OpeningB,
    double OpeningH);

public sealed record VerificatieActiefPaneelModel(
    PaneelResultaat Resultaat,
    PaneelToewijzing? Toewijzing,
    IReadOnlyList<Kast> PaneelKasten,
    VerificatieChecklistStatus Checklist,
    double OpeningB,
    double OpeningH,
    double PotHartVanRand,
    int AfgerondePanelen,
    int OpenPaneelControles,
    string WandNaam,
    int TotaalPanelen);

public sealed record VerificatieAfrondingModel(int AantalGeverifieerd, int TotaalPanelen)
{
    public bool AlleGeverifieerd => AantalGeverifieerd == TotaalPanelen;
}
