using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class Verificatie
{
    private VerificatiePaginaModel MaakPaginaModel()
    {
        var flowStatus = StappenFlowHelper.BepaalStatus(State);
        var resultaten = State.BerekenResultaten();

        return new VerificatiePaginaModel(
            StappenFlowHelper.BepaalRouteGate("verificatie", flowStatus),
            resultaten,
            MaakOverzichtModel(resultaten),
            MaakActiefPaneelModel(resultaten),
            MaakAfrondingModel(resultaten));
    }

    private VerificatieOverzichtModel? MaakOverzichtModel(IReadOnlyList<PaneelResultaat> resultaten)
    {
        if (resultaten.Count == 0)
            return null;

        var taken = resultaten
            .Select(MaakPaneelTaakModel)
            .ToList();

        var taakGroepen = State.Wanden
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

        return new VerificatieOverzichtModel(
            taakGroepen,
            aantalAfgerond,
            eersteOngecontroleerdIndex,
            taken.Sum(taak => taak.OpenChecks),
            aantalAfgerond > 0,
            samenvattingTaak);
    }

    private VerificatiePaneelTaakModel MaakPaneelTaakModel(PaneelResultaat resultaat, int index)
    {
        var wandId = resultaat.KastIds
            .Select(State.WandVoorKast)
            .FirstOrDefault(kandidaat => kandidaat is not null)?.Id;

        return new VerificatiePaneelTaakModel(
            index,
            resultaat,
            wandId,
            State.WandNaamVoorKasten(resultaat.KastIds),
            AlleChecksVoorPaneel(index, resultaat),
            OpenChecksVoorPaneel(index, resultaat),
            VolgendeControleTitel(index, resultaat),
            resultaat.MaatInfo?.OpeningsRechthoek.Breedte ?? resultaat.Breedte,
            resultaat.MaatInfo?.OpeningsRechthoek.Hoogte ?? resultaat.Hoogte);
    }

    private VerificatieActiefPaneelModel? MaakActiefPaneelModel(IReadOnlyList<PaneelResultaat> resultaten)
    {
        if (_fase != VerificatieFase.PaneelVerificatie || _paneelIndex >= resultaten.Count)
            return null;

        var resultaat = resultaten[_paneelIndex];
        var toewijzing = State.Toewijzingen.FirstOrDefault(t => t.Id == resultaat.ToewijzingId);
        var checks = GetChecks(resultaat);
        var isDeur = resultaat.Type == PaneelType.Deur && resultaat.Boorgaten.Count > 0;

        return new VerificatieActiefPaneelModel(
            resultaat,
            toewijzing,
            State.ZoekKasten(resultaat.KastIds),
            checks,
            isDeur,
            resultaat.MaatInfo?.OpeningsRechthoek.Breedte ?? resultaat.Breedte,
            resultaat.MaatInfo?.OpeningsRechthoek.Hoogte ?? resultaat.Hoogte,
            resultaat.Boorgaten.FirstOrDefault()?.X
                ?? toewijzing?.PotHartVanRand
                ?? State.LaatstGebruiktePotHartVanRand,
            AantalAfgevinktVoorPaneel(_paneelIndex, resultaat),
            isDeur ? 2 : 1,
            Enumerable.Range(0, resultaten.Count).Count(i => AlleChecksVoorPaneel(i, resultaten[i])),
            Enumerable.Range(0, resultaten.Count).Sum(i => OpenChecksVoorPaneel(i, resultaten[i])),
            State.WandNaamVoorKasten(resultaat.KastIds),
            VolgendeControleTitel(_paneelIndex, resultaat),
            HuidigeControleHint(_paneelIndex, resultaat),
            OpenChecksLabel(_paneelIndex, resultaat));
    }

    private VerificatieAfrondingModel? MaakAfrondingModel(IReadOnlyList<PaneelResultaat> resultaten)
    {
        if (_fase != VerificatieFase.Afronding)
            return null;

        return new VerificatieAfrondingModel(
            Enumerable.Range(0, resultaten.Count).Count(i => AlleChecksVoorPaneel(i, resultaten[i])),
            resultaten.Count);
    }

    private sealed record VerificatiePaginaModel(
        StapRouteGate? RouteGate,
        IReadOnlyList<PaneelResultaat> Resultaten,
        VerificatieOverzichtModel? Overzicht,
        VerificatieActiefPaneelModel? ActiefPaneel,
        VerificatieAfrondingModel? Afronding);

    private sealed record VerificatieOverzichtModel(
        IReadOnlyList<VerificatieTaakGroepModel> TaakGroepen,
        int AantalAfgerond,
        int EersteOngecontroleerdIndex,
        int TotaalOpenChecks,
        bool AlGestart,
        VerificatiePaneelTaakModel SamenvattingTaak);

    private sealed record VerificatieTaakGroepModel(
        Guid WandId,
        string WandNaam,
        IReadOnlyList<VerificatiePaneelTaakModel> Taken)
    {
        public int OpenTaken => Taken.Count(taak => !taak.Geverifieerd);
        public int AfgerondeTaken => Taken.Count(taak => taak.Geverifieerd);
    }

    private sealed record VerificatiePaneelTaakModel(
        int Index,
        PaneelResultaat Resultaat,
        Guid? WandId,
        string WandNaam,
        bool Geverifieerd,
        int OpenChecks,
        string VolgendeControle,
        double OpeningB,
        double OpeningH);

    private sealed record VerificatieActiefPaneelModel(
        PaneelResultaat Resultaat,
        PaneelToewijzing? Toewijzing,
        IReadOnlyList<Kast> PaneelKasten,
        PaneelVerificatieStatus Checks,
        bool IsDeur,
        double OpeningB,
        double OpeningH,
        double PotHartVanRand,
        int AantalAfgevinkt,
        int TotaalChecks,
        int AfgerondePanelen,
        int OpenPaneelControles,
        string WandNaam,
        string VolgendeControle,
        string HuidigeControleHint,
        string OpenChecksLabel);

    private sealed record VerificatieAfrondingModel(int AantalGeverifieerd, int TotaalPanelen)
    {
        public bool AlleGeverifieerd => AantalGeverifieerd == TotaalPanelen;
    }
}
