namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class KeukenShareCodec
{
    private static Dictionary<Guid, double> BerekenStandaardKastPosities(KeukenData data)
    {
        var kastenOpId = data.Kasten.ToDictionary(kast => kast.Id);
        var standaard = new Dictionary<Guid, double>();

        foreach (var wand in data.Wanden)
        {
            double lopendeX = 0;
            foreach (var kastId in wand.KastIds)
            {
                if (!kastenOpId.TryGetValue(kastId, out var kast))
                    continue;

                standaard[kastId] = Round1(lopendeX);
                lopendeX += kast.Breedte;
            }
        }

        return standaard;
    }

    private static Dictionary<Guid, (double xPositie, double hoogteVanVloer)> BerekenStandaardApparaatPlaatsingen(KeukenData data)
    {
        var kastenOpId = data.Kasten.ToDictionary(kast => kast.Id);
        var apparatenOpId = data.Apparaten.ToDictionary(apparaat => apparaat.Id);
        var standaard = new Dictionary<Guid, (double xPositie, double hoogteVanVloer)>();

        foreach (var wand in data.Wanden)
        {
            var wandKasten = wand.KastIds
                .Select(id => kastenOpId.GetValueOrDefault(id))
                .Where(kast => kast is not null)
                .Cast<Kast>()
                .ToList();
            var geplaatsteApparaten = new List<Apparaat>();

            foreach (var apparaatId in wand.ApparaatIds)
            {
                if (!apparatenOpId.TryGetValue(apparaatId, out var apparaat))
                    continue;

                var plaatsing = ApparaatLayoutService.BepaalStandaardPlaatsing(
                    wand,
                    apparaat,
                    wandKasten,
                    geplaatsteApparaten);
                standaard[apparaatId] = plaatsing;
                geplaatsteApparaten.Add(MaakGeplaatstApparaat(apparaat, plaatsing));
            }
        }

        return standaard;
    }

    private static Dictionary<int, double> BerekenDecodeStandaardKastPosities(CompactShareData data)
    {
        var walls = data.Walls ?? [];
        var cabinets = data.Cabinets ?? [];
        var standaard = new Dictionary<int, double>();

        foreach (var wand in walls)
        {
            double lopendeX = 0;
            foreach (var index in wand.CabinetIndexes ?? [])
            {
                if (index < 0 || index >= cabinets.Count)
                    continue;

                standaard[index] = Round1(lopendeX);
                lopendeX += cabinets[index].Width ?? DefaultKast.Breedte;
            }
        }

        return standaard;
    }

    private static Dictionary<int, (double xPositie, double hoogteVanVloer)> BerekenDecodeStandaardApparaatPlaatsingen(
        CompactShareData data,
        IReadOnlyDictionary<int, double> defaultCabinetX)
    {
        var walls = data.Walls ?? [];
        var cabinets = data.Cabinets ?? [];
        var appliances = data.Appliances ?? [];
        var standaard = new Dictionary<int, (double xPositie, double hoogteVanVloer)>();

        foreach (var wand in walls)
        {
            var wandModel = new KeukenWand
            {
                Id = Guid.NewGuid(),
                Naam = wand.Name ?? "",
                Breedte = wand.Width ?? DefaultWand.Breedte,
                Hoogte = wand.Height ?? DefaultWand.Hoogte,
                PlintHoogte = wand.PlinthHeight ?? DefaultWand.PlintHoogte
            };
            var wandKasten = (wand.CabinetIndexes ?? [])
                .Where(index => index >= 0 && index < cabinets.Count)
                .Select(index => BouwKast(cabinets[index], Guid.NewGuid(), defaultCabinetX.GetValueOrDefault(index, 0)))
                .ToList();
            var geplaatsteApparaten = new List<Apparaat>();

            foreach (var index in wand.ApplianceIndexes ?? [])
            {
                if (index < 0 || index >= appliances.Count)
                    continue;

                var type = appliances[index].Type is int typeNummer ? (ApparaatType)typeNummer : ApparaatType.Oven;
                var standaardAfmetingen = Apparaat.StandaardAfmetingen(type);
                var apparaat = new Apparaat
                {
                    Id = Guid.NewGuid(),
                    Naam = appliances[index].Name ?? "",
                    Type = type,
                    Breedte = appliances[index].Width ?? standaardAfmetingen.breedte,
                    Hoogte = appliances[index].Height ?? standaardAfmetingen.hoogte,
                    Diepte = appliances[index].Depth ?? standaardAfmetingen.diepte
                };
                var plaatsing = ApparaatLayoutService.BepaalStandaardPlaatsing(
                    wandModel,
                    apparaat,
                    wandKasten,
                    geplaatsteApparaten);
                standaard[index] = plaatsing;
                geplaatsteApparaten.Add(MaakGeplaatstApparaat(apparaat, plaatsing));
            }
        }

        return standaard;
    }

    private static Apparaat MaakGeplaatstApparaat(Apparaat bron, (double xPositie, double hoogteVanVloer) plaatsing)
        => new()
        {
            Id = bron.Id,
            Naam = bron.Naam,
            Type = bron.Type,
            Breedte = bron.Breedte,
            Hoogte = bron.Hoogte,
            Diepte = bron.Diepte,
            XPositie = plaatsing.xPositie,
            HoogteVanVloer = plaatsing.hoogteVanVloer
        };
}
