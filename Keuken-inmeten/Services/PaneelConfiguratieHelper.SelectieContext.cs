namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class PaneelConfiguratieHelper
{
    public static PaneelSelectieContext BouwSelectieContext(
        Guid? actieveWandId,
        string actieveWandNaam,
        IEnumerable<Kast> geselecteerdeKasten,
        PaneelWerkruimteContext? werkruimte,
        bool isBewerkModus)
    {
        var geselecteerdeKastLijst = geselecteerdeKasten.ToList();
        PaneelRechthoek? selectieBereik = PaneelLayoutService.BerekenOmhullende(geselecteerdeKastLijst);
        IReadOnlyList<PaneelRechthoek> bestaandePaneelRechthoeken = werkruimte?.PaneelBronnen
            .Select(bron => bron.OpeningsRechthoek.Kopie())
            .ToList() ?? [];
        IReadOnlyList<PaneelRechthoek> vrijeSegmenten = selectieBereik is null
            ? []
            : BepaalVrijeSegmenten(selectieBereik, bestaandePaneelRechthoeken);
        PaneelRechthoek? opdeelBereik = !isBewerkModus && geselecteerdeKastLijst.Count == 1 && selectieBereik is not null
            ? BepaalOpdeelBereik(selectieBereik, bestaandePaneelRechthoeken)
            : null;

        return new PaneelSelectieContext(
            ActieveWandId: actieveWandId,
            ActieveWandNaam: string.IsNullOrWhiteSpace(actieveWandNaam) ? "—" : actieveWandNaam,
            GeselecteerdeKasten: geselecteerdeKastLijst,
            Werkruimte: werkruimte,
            SelectieBereik: selectieBereik,
            BestaandePaneelRechthoeken: bestaandePaneelRechthoeken,
            VrijeSegmenten: vrijeSegmenten,
            OpdeelBereik: opdeelBereik,
            GeselecteerdeKastNamen: string.Join(" + ", geselecteerdeKastLijst.Select(kast => kast.Naam)));
    }
}

public sealed record PaneelSelectieContext(
    Guid? ActieveWandId,
    string ActieveWandNaam,
    IReadOnlyList<Kast> GeselecteerdeKasten,
    PaneelWerkruimteContext? Werkruimte,
    PaneelRechthoek? SelectieBereik,
    IReadOnlyList<PaneelRechthoek> BestaandePaneelRechthoeken,
    IReadOnlyList<PaneelRechthoek> VrijeSegmenten,
    PaneelRechthoek? OpdeelBereik,
    string GeselecteerdeKastNamen)
{
    public bool HeeftSelectie => GeselecteerdeKasten.Count > 0;
    public bool HeeftEnkeleKastSelectie => GeselecteerdeKasten.Count == 1;
    public Kast? EnkeleGeselecteerdeKast => GeselecteerdeKasten.Count == 1 ? GeselecteerdeKasten[0] : null;
}
