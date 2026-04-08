namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class PaneelGeometrieService
{
    public static PaneelGeometrieBron? MaakBronVoorToewijzing(PaneelToewijzing toewijzing, IEnumerable<Kast> kastenBron)
    {
        var kasten = kastenBron.ToList();
        var openingsRechthoek = PaneelLayoutService.BerekenRechthoek(toewijzing, kasten);
        return openingsRechthoek is null
            ? null
            : new PaneelGeometrieBron(toewijzing.Id, openingsRechthoek.Kopie());
    }

    public static PaneelGeometrieResultaat? BerekenVoorToewijzing(
        PaneelToewijzing toewijzing,
        IEnumerable<Kast> kastenVoorOpening,
        IEnumerable<Kast> wandKasten,
        IEnumerable<Apparaat> wandApparaten,
        IEnumerable<PaneelGeometrieBron> wandPaneelBronnen,
        double randSpelingPerRaakrand)
    {
        var bron = MaakBronVoorToewijzing(toewijzing, kastenVoorOpening);
        return bron is null
            ? null
            : Bereken(bron, wandKasten, wandApparaten, wandPaneelBronnen, randSpelingPerRaakrand);
    }

    public static PaneelGeometrieResultaat? BerekenVoorConceptPaneel(
        PaneelRechthoek conceptPaneel,
        IEnumerable<Kast> wandKasten,
        IEnumerable<Apparaat> wandApparaten,
        IEnumerable<PaneelGeometrieBron> wandPaneelBronnen,
        double randSpelingPerRaakrand,
        Guid? paneelId = null)
    {
        var resultaat = Bereken(
            new PaneelGeometrieBron(paneelId, conceptPaneel.Kopie()),
            wandKasten,
            wandApparaten,
            wandPaneelBronnen,
            randSpelingPerRaakrand);

        return resultaat.DragendeKasten.Count == 0 ? null : resultaat;
    }

    public static PaneelGeometrieResultaat Bereken(
        PaneelGeometrieBron bron,
        IEnumerable<Kast> wandKastenBron,
        IEnumerable<Apparaat> wandApparatenBron,
        IEnumerable<PaneelGeometrieBron> wandPaneelBronnen,
        double randSpelingPerRaakrand)
    {
        var wandKasten = wandKastenBron.ToList();
        var dragendeKasten = PaneelLayoutService.BepaalOverlappendeKasten(wandKasten, bron.OpeningsRechthoek);
        var dragendeKastIds = dragendeKasten.Select(kast => kast.Id).ToHashSet();

        var buurKasten = wandKasten
            .Where(kast => !dragendeKastIds.Contains(kast.Id))
            .Select(PaneelLayoutService.NaarRechthoek);
        var buurApparaten = wandApparatenBron.Select(PaneelLayoutService.NaarRechthoek);
        var buurPanelen = wandPaneelBronnen
            .Where(paneel => bron.PaneelId is null || paneel.PaneelId != bron.PaneelId)
            .Select(paneel => paneel.OpeningsRechthoek.Kopie());

        List<PaneelRechthoek> buurRechthoeken = [.. buurKasten, .. buurApparaten, .. buurPanelen];
        var maatInfo = PaneelSpelingService.BerekenMaatInfo(bron.OpeningsRechthoek, buurRechthoeken, randSpelingPerRaakrand);
        List<Kast> dragendeKastenResultaat = [.. dragendeKasten];

        return new PaneelGeometrieResultaat(maatInfo, dragendeKastenResultaat, buurRechthoeken);
    }
}

public sealed record PaneelGeometrieBron(Guid? PaneelId, PaneelRechthoek OpeningsRechthoek);

public sealed record PaneelGeometrieResultaat(
    PaneelMaatInfo MaatInfo,
    IReadOnlyList<Kast> DragendeKasten,
    IReadOnlyList<PaneelRechthoek> BuurRechthoeken)
{
    public PaneelRechthoek OpeningsRechthoek => MaatInfo.OpeningsRechthoek;
    public PaneelRechthoek WerkRechthoek => MaatInfo.PaneelRechthoek;
}
