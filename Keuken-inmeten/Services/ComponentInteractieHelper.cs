using Keuken_inmeten.Models;

namespace Keuken_inmeten.Services;

public static class ComponentInteractieHelper
{
    public static KastPositieWijziging MaakKastVerplaatsing(Guid kastId, double xPositie, double hoogteVanVloer)
        => new(kastId, xPositie, hoogteVanVloer);

    public static ApparaatPositieWijziging MaakApparaatVerplaatsing(Guid apparaatId, double xPositie, double hoogteVanVloer)
        => new(apparaatId, xPositie, hoogteVanVloer);

    public static KastVolgordeWijziging? BepaalKastVolgordeWijziging(int vanIndex, int targetIndex)
    {
        if (vanIndex < 0 || targetIndex < 0)
            return null;

        var effectieveDoelIndex = targetIndex > vanIndex ? targetIndex - 1 : targetIndex;
        return effectieveDoelIndex == vanIndex
            ? null
            : new KastVolgordeWijziging(vanIndex, effectieveDoelIndex);
    }

    public static WandPlankActie MaakPlankToevoeging(Guid kastId, Guid plankId, double hoogteVanBodem)
        => new(WandPlankActieType.Toevoegen, kastId, plankId, hoogteVanBodem);

    public static WandPlankActie MaakPlankVerplaatsing(Guid kastId, Guid plankId, double hoogteVanBodem)
        => new(WandPlankActieType.Verplaatsen, kastId, plankId, hoogteVanBodem);

    public static WandPlankActie MaakPlankVerwijdering(Guid kastId, Guid plankId, double hoogteVanBodem, int index)
        => new(WandPlankActieType.Verwijderen, kastId, plankId, hoogteVanBodem, index);

    public static WandPlankActie MaakPlankHerstel(Guid kastId, Guid plankId, double hoogteVanBodem, int index)
        => new(WandPlankActieType.Herstellen, kastId, plankId, hoogteVanBodem, index);

    public static PaneelConceptWijziging MaakPaneelConceptWijziging(
        string bewerking,
        double svgX,
        double svgY,
        double svgWidth,
        double svgHeight,
        double padding,
        double schaal,
        double vloerY)
    {
        var xMm = (svgX - padding) / schaal;
        var topFromFloorMm = (vloerY - svgY) / schaal;
        var breedteMm = svgWidth / schaal;
        var hoogteMm = svgHeight / schaal;

        return new PaneelConceptWijziging
        {
            Bewerking = bewerking,
            Paneel = new PaneelRechthoek
            {
                XPositie = xMm,
                HoogteVanVloer = topFromFloorMm - hoogteMm,
                Breedte = breedteMm,
                Hoogte = hoogteMm
            }
        };
    }
}
