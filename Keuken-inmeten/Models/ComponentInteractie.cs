namespace Keuken_inmeten.Models;

public sealed record KastPositieWijziging(Guid KastId, double XPositie, double HoogteVanVloer);

public sealed record ApparaatPositieWijziging(Guid ApparaatId, double XPositie, double HoogteVanVloer);

public sealed record KastVolgordeWijziging(int VanIndex, int NaarIndex);

public enum WandPlankActieType
{
    Toevoegen,
    Verplaatsen,
    Verwijderen,
    Herstellen
}

public sealed record WandPlankActie(
    WandPlankActieType Type,
    Guid KastId,
    Guid PlankId,
    double HoogteVanBodem,
    int Index = -1);
