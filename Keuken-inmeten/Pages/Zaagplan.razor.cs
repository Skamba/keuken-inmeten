using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class Zaagplan
{
    private enum ZaagplanWeergave
    {
        AllePlaten,
        EenPlaat
    }

    private double _plaatBreedte = 2800;
    private double _plaatHoogte = 2070;
    private double _zaagbreedte = 4;
    private bool _draaiToe = true;
    private int _actievePlaatIndex;
    private ZaagplanWeergave _weergave = ZaagplanWeergave.AllePlaten;

    private ZaagplanPaginaModel MaakPaginaModel()
    {
        var flowStatus = StappenFlowHelper.BepaalStatus(State);
        var routeGate = StappenFlowHelper.BepaalRouteGate("zaagplan", flowStatus);
        var items = BestellijstService.BerekenItems(State);
        var totaalPanelen = items.Sum(item => item.Aantal);

        if (routeGate is not null || items.Count == 0)
        {
            return new ZaagplanPaginaModel(
                RouteGate: routeGate,
                Items: items,
                Resultaat: null,
                TotaalPanelen: totaalPanelen,
                TotaalGeplaatst: 0,
                NietGeplaatstAantal: 0,
                KanPlaatFocus: false,
                IsPlaatFocus: false,
                ActievePlaatIndex: 0,
                ActievePlaat: null,
                PlatenVoorWeergave: [],
                GrootsteNietGeplaatst: null,
                RotatieKanHelpen: false,
                MinimalePlaatLangeZijde: 0,
                MinimalePlaatKorteZijde: 0);
        }

        var resultaat = ZaagplanService.Bereken(
            ZaagplanService.VanBestellijst(items),
            _plaatBreedte,
            _plaatHoogte,
            _zaagbreedte,
            _draaiToe);
        var kanPlaatFocus = resultaat.Platen.Count > 0;
        var isPlaatFocus = _weergave is ZaagplanWeergave.EenPlaat && kanPlaatFocus;
        var actievePlaatIndex = ActievePlaatIndex(resultaat.Platen.Count);
        var actievePlaat = isPlaatFocus ? resultaat.Platen[actievePlaatIndex] : null;
        var grootsteNietGeplaatst = resultaat.NietGeplaatst
            .OrderByDescending(paneel => Math.Max(paneel.Breedte, paneel.Hoogte))
            .ThenByDescending(paneel => Math.Min(paneel.Breedte, paneel.Hoogte))
            .FirstOrDefault();

        return new ZaagplanPaginaModel(
            RouteGate: null,
            Items: items,
            Resultaat: resultaat,
            TotaalPanelen: totaalPanelen,
            TotaalGeplaatst: resultaat.Platen.Sum(plaat => plaat.Plaatsingen.Count),
            NietGeplaatstAantal: resultaat.NietGeplaatst.Sum(paneel => paneel.Aantal),
            KanPlaatFocus: kanPlaatFocus,
            IsPlaatFocus: isPlaatFocus,
            ActievePlaatIndex: actievePlaatIndex,
            ActievePlaat: actievePlaat,
            PlatenVoorWeergave: actievePlaat is null ? resultaat.Platen : [actievePlaat],
            GrootsteNietGeplaatst: grootsteNietGeplaatst,
            RotatieKanHelpen: !_draaiToe && resultaat.NietGeplaatst.Any(paneel => PastNaDraaien(paneel, _plaatBreedte, _plaatHoogte)),
            MinimalePlaatLangeZijde: grootsteNietGeplaatst is null ? 0 : Math.Max(grootsteNietGeplaatst.Breedte, grootsteNietGeplaatst.Hoogte),
            MinimalePlaatKorteZijde: grootsteNietGeplaatst is null ? 0 : Math.Min(grootsteNietGeplaatst.Breedte, grootsteNietGeplaatst.Hoogte));
    }

    private void ToonAllePlaten() => _weergave = ZaagplanWeergave.AllePlaten;

    private void ToonEenPlaat(int aantalPlaten)
    {
        if (aantalPlaten == 0)
            return;

        _weergave = ZaagplanWeergave.EenPlaat;
        _actievePlaatIndex = Math.Min(_actievePlaatIndex, Math.Max(0, aantalPlaten - 1));
    }

    private void VorigePlaat() => _actievePlaatIndex = Math.Max(0, _actievePlaatIndex - 1);

    private void VolgendePlaat(int aantalPlaten)
    {
        if (aantalPlaten == 0)
            return;

        _actievePlaatIndex = Math.Min(_actievePlaatIndex + 1, aantalPlaten - 1);
    }

    private int ActievePlaatIndex(int aantalPlaten)
        => aantalPlaten == 0 ? 0 : Math.Clamp(_actievePlaatIndex, 0, aantalPlaten - 1);

    private void SchakelDraaienIn() => _draaiToe = true;

    private static bool PastNaDraaien(ZaagplanPaneel paneel, double plaatBreedte, double plaatHoogte)
        => paneel.Hoogte <= plaatBreedte && paneel.Breedte <= plaatHoogte;

    private static string FormatPlaatTeller(int index, int totaal)
        => totaal == 0 ? "Geen platen" : $"Plaat {index + 1} van {totaal}";

    private static string FormatMm(double value) => $"{value:0.#}";

    private sealed record ZaagplanPaginaModel(
        StapRouteGate? RouteGate,
        IReadOnlyList<BestellijstItem> Items,
        ZaagplanResultaat? Resultaat,
        int TotaalPanelen,
        int TotaalGeplaatst,
        int NietGeplaatstAantal,
        bool KanPlaatFocus,
        bool IsPlaatFocus,
        int ActievePlaatIndex,
        ZaagplanPlaat? ActievePlaat,
        IReadOnlyList<ZaagplanPlaat> PlatenVoorWeergave,
        ZaagplanPaneel? GrootsteNietGeplaatst,
        bool RotatieKanHelpen,
        double MinimalePlaatLangeZijde,
        double MinimalePlaatKorteZijde);
}
