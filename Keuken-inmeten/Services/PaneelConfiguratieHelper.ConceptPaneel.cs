namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class PaneelConfiguratieHelper
{
    public static PaneelConceptState BouwConceptStartState(
        PaneelRechthoek? selectieBereik,
        IReadOnlyList<PaneelRechthoek> vrijeSegmenten)
        => selectieBereik is null
            ? PaneelConceptState.Leeg
            : BouwConceptState(BepaalStartRechthoek(selectieBereik, vrijeSegmenten));

    public static PaneelConceptState BouwConceptStateVoorBron(PaneelGeometrieBron? paneelBron)
        => paneelBron is null
            ? PaneelConceptState.Leeg
            : BouwConceptState(paneelBron.OpeningsRechthoek.Kopie());

    public static PaneelConceptState BouwConceptStateVoorSegment(PaneelRechthoek segment)
        => BouwConceptState(segment.Kopie());

    public static PaneelConceptState? VerwerkConceptWijziging(
        PaneelConceptWijziging wijziging,
        PaneelSelectieContext selectieContext)
    {
        if (selectieContext.SelectieBereik is not { } selectieBereik)
            return null;

        var voorstel = PaneelLayoutService.ClampBinnen(wijziging.Paneel, selectieBereik);
        var conceptPaneel = wijziging.Bewerking.StartsWith("input-", StringComparison.Ordinal)
            ? voorstel
            : SnapPaneel(
                wijziging.Bewerking,
                voorstel,
                selectieBereik,
                XTargets(selectieBereik, selectieContext),
                YTargets(selectieBereik, selectieContext));

        return BouwConceptState(conceptPaneel);
    }

    private static PaneelConceptState BouwConceptState(PaneelRechthoek conceptPaneel)
        => new(
            ConceptPaneel: conceptPaneel,
            XPositie: Math.Round(conceptPaneel.XPositie, 1),
            HoogteVanVloer: Math.Round(conceptPaneel.HoogteVanVloer, 1),
            Breedte: Math.Round(conceptPaneel.Breedte, 1),
            Hoogte: Math.Round(conceptPaneel.Hoogte, 1));

    private static IEnumerable<double> XTargets(PaneelRechthoek selectieBereik, PaneelSelectieContext selectieContext)
    {
        yield return selectieBereik.XPositie;
        yield return selectieBereik.Rechterkant;

        foreach (var kast in selectieContext.GeselecteerdeKasten)
        {
            yield return kast.XPositie;
            yield return kast.XPositie + kast.Breedte;
        }

        foreach (var paneel in selectieContext.BestaandePaneelRechthoeken)
        {
            yield return paneel.XPositie;
            yield return paneel.Rechterkant;
        }
    }

    private static IEnumerable<double> YTargets(PaneelRechthoek selectieBereik, PaneelSelectieContext selectieContext)
    {
        yield return selectieBereik.HoogteVanVloer;
        yield return selectieBereik.Bovenzijde;

        foreach (var kast in selectieContext.GeselecteerdeKasten)
        {
            yield return kast.HoogteVanVloer;
            yield return kast.HoogteVanVloer + kast.Hoogte;
        }

        foreach (var paneel in selectieContext.BestaandePaneelRechthoeken)
        {
            yield return paneel.HoogteVanVloer;
            yield return paneel.Bovenzijde;
        }
    }
}

public sealed record PaneelConceptState(
    PaneelRechthoek? ConceptPaneel,
    double? XPositie,
    double? HoogteVanVloer,
    double Breedte,
    double Hoogte)
{
    public static PaneelConceptState Leeg { get; } = new(
        ConceptPaneel: null,
        XPositie: null,
        HoogteVanVloer: null,
        Breedte: 0,
        Hoogte: 0);
}
