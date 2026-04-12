namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class IndelingFormulierHelper
{
    public static Kast KopieerKast(Kast bron, bool behoudPlankIds = true) => new()
    {
        Id = bron.Id,
        Naam = bron.Naam,
        Type = bron.Type,
        Breedte = bron.Breedte,
        Hoogte = bron.Hoogte,
        Diepte = bron.Diepte,
        Wanddikte = bron.Wanddikte,
        GaatjesAfstand = bron.GaatjesAfstand,
        EersteGaatVanBoven = bron.EersteGaatVanBoven,
        HoogteVanVloer = bron.HoogteVanVloer,
        XPositie = bron.XPositie,
        MontagePlaatPosities = bron.MontagePlaatPosities
            .Select(positie => new MontagePlaatPositie
            {
                AfstandVanBoven = positie.AfstandVanBoven,
                Zijde = positie.Zijde
            })
            .ToList(),
        Planken = bron.Planken
            .Select(plank => new Plank
            {
                Id = behoudPlankIds ? plank.Id : Guid.NewGuid(),
                HoogteVanBodem = plank.HoogteVanBodem
            })
            .ToList()
    };

    public static Apparaat KopieerApparaat(Apparaat bron) => new()
    {
        Id = bron.Id,
        Naam = bron.Naam,
        Type = bron.Type,
        Breedte = bron.Breedte,
        Hoogte = bron.Hoogte,
        Diepte = bron.Diepte,
        HoogteVanVloer = bron.HoogteVanVloer,
        XPositie = bron.XPositie
    };

    public static PaneelToewijzing KopieerToewijzing(PaneelToewijzing bron) => new()
    {
        Id = bron.Id,
        KastIds = [.. bron.KastIds],
        Type = bron.Type,
        ScharnierZijde = bron.ScharnierZijde,
        PotHartVanRand = bron.PotHartVanRand,
        Breedte = bron.Breedte,
        Hoogte = bron.Hoogte,
        XPositie = bron.XPositie,
        HoogteVanVloer = bron.HoogteVanVloer
    };
}
