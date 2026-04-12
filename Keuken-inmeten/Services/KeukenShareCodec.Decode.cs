namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class KeukenShareCodec
{
    private static KeukenData BouwKeukenData(CompactShareData data, int schemaVersie)
    {
        var walls = data.Walls ?? [];
        var cabinets = data.Cabinets ?? [];
        var appliances = data.Appliances ?? [];
        var panels = data.Panels ?? [];
        var wallIds = walls.Select(_ => Guid.NewGuid()).ToList();
        var cabinetIds = cabinets.Select(_ => Guid.NewGuid()).ToList();
        var applianceIds = appliances.Select(_ => Guid.NewGuid()).ToList();
        var defaultCabinetX = BerekenDecodeStandaardKastPosities(data);
        var defaultAppliancePlaatsingen = BerekenDecodeStandaardApparaatPlaatsingen(data, defaultCabinetX);
        var paneelRandSpeling = schemaVersie >= KeukenDataMigratieService.HuidigeSchemaVersie
            ? PaneelSpelingService.NormaliseerRandSpeling(data.PanelEdgeClearance ?? KeukenDomeinDefaults.ProjectDefaults.PaneelRandSpeling)
            : PaneelSpelingService.NormaliseerLegacyRandSpeling(data.PanelEdgeClearance ?? PaneelSpelingService.LegacyDefaultRandSpeling);

        var wanden = walls.Select((wand, index) => new KeukenWand
        {
            Id = wallIds[index],
            Naam = wand.Name ?? "",
            Breedte = wand.Width ?? DefaultWand.Breedte,
            Hoogte = wand.Height ?? DefaultWand.Hoogte,
            PlintHoogte = wand.PlinthHeight ?? DefaultWand.PlintHoogte,
            KastIds = MapIds(wand.CabinetIndexes, cabinetIds),
            ApparaatIds = MapIds(wand.ApplianceIndexes, applianceIds)
        }).ToList();

        var kasten = cabinets.Select((kast, index) => BouwKast(kast, cabinetIds[index], defaultCabinetX.GetValueOrDefault(index, 0))).ToList();
        var apparaten = appliances.Select((apparaat, index) =>
        {
            var standaardPlaatsing = defaultAppliancePlaatsingen.GetValueOrDefault(index, (0, 0));
            return BouwApparaat(apparaat, applianceIds[index], standaardPlaatsing);
        }).ToList();
        var kastenOpId = kasten.ToDictionary(kast => kast.Id);

        var toewijzingen = panels.Select(panel =>
        {
            var kastIds = MapIds(panel.CabinetIndexes, cabinetIds);
            var panelKasten = kastIds
                .Select(id => kastenOpId.GetValueOrDefault(id))
                .Where(kast => kast is not null)
                .Cast<Kast>()
                .ToList();
            var omhullende = PaneelLayoutService.BerekenOmhullende(panelKasten);
            var heeftExplicietePlaatsing = panel.X is not null || panel.FloorHeight is not null || panel.Width is not null || panel.Height is not null;

            return new PaneelToewijzing
            {
                Id = Guid.NewGuid(),
                KastIds = kastIds,
                Type = panel.Type is int panelType ? (PaneelType)panelType : PaneelType.Deur,
                ScharnierZijde = panel.HingeSide is int zijde ? (ScharnierZijde)zijde : ScharnierZijde.Links,
                PotHartVanRand = panel.CupOffset ?? KeukenDomeinDefaults.ProjectDefaults.LaatstGebruiktePotHartVanRand,
                Breedte = Round1(panel.Width ?? omhullende?.Breedte ?? 0),
                Hoogte = Round1(panel.Height ?? omhullende?.Hoogte ?? 0),
                XPositie = heeftExplicietePlaatsing ? Round1(panel.X ?? omhullende?.XPositie ?? 0) : null,
                HoogteVanVloer = heeftExplicietePlaatsing ? Round1(panel.FloorHeight ?? omhullende?.HoogteVanVloer ?? 0) : null
            };
        }).ToList();

        var verificatieStatussen = panels
            .Select((panel, index) => DecodeVerificatieStatus(panel.VerificationStatus, toewijzingen[index].Id))
            .Where(status => status is not null)
            .Cast<PaneelVerificatieStatus>()
            .ToList();

        return new KeukenData
        {
            Wanden = wanden,
            Kasten = kasten,
            Apparaten = apparaten,
            Toewijzingen = toewijzingen,
            VerificatieStatussen = verificatieStatussen,
            KastTemplates = [],
            LaatstGebruiktePotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(data.LastCupOffset ?? KeukenDomeinDefaults.ProjectDefaults.LaatstGebruiktePotHartVanRand),
            PaneelRandSpeling = paneelRandSpeling
        };
    }

    private static PaneelVerificatieStatus? DecodeVerificatieStatus(int? bits, Guid toewijzingId)
    {
        if (bits is null || bits.Value == 0)
            return null;

        return new PaneelVerificatieStatus
        {
            ToewijzingId = toewijzingId,
            MatenOk = (bits.Value & 1) != 0,
            ScharnierPositiesOk = (bits.Value & 2) != 0
        };
    }

    private static Kast BouwKast(CompactCabinet bron, Guid id, double standaardX)
    {
        var hoogte = bron.Height ?? DefaultKast.Hoogte;
        var wanddikte = bron.WallThickness ?? DefaultKast.Wanddikte;
        var eersteGaatVanBoven = bron.FirstHoleBelowTopShelf ?? DefaultKast.EersteGaatVanBoven;
        var gaatjesAfstand = bron.HoleSpacing ?? DefaultKast.GaatjesAfstand;

        return new Kast
        {
            Id = id,
            Naam = bron.Name ?? "",
            Type = InferKastType(hoogte),
            Breedte = bron.Width ?? DefaultKast.Breedte,
            Hoogte = hoogte,
            Diepte = bron.Depth ?? DefaultKast.Diepte,
            Wanddikte = wanddikte,
            GaatjesAfstand = gaatjesAfstand,
            EersteGaatVanBoven = eersteGaatVanBoven,
            HoogteVanVloer = bron.FloorHeight ?? 0,
            XPositie = bron.X ?? standaardX,
            MontagePlaatPosities = BouwMontagePlaatPosities(hoogte, wanddikte, eersteGaatVanBoven, gaatjesAfstand),
            Planken = bron.ShelfHeights?.Select(hoogteVanBodem => new Plank { HoogteVanBodem = hoogteVanBodem }).ToList() ?? []
        };
    }

    private static Apparaat BouwApparaat(CompactAppliance bron, Guid id, (double xPositie, double hoogteVanVloer) standaardPlaatsing)
    {
        var type = bron.Type is int typeNummer ? (ApparaatType)typeNummer : ApparaatType.Oven;
        var standaardAfmetingen = Apparaat.StandaardAfmetingen(type);

        return new Apparaat
        {
            Id = id,
            Naam = bron.Name ?? "",
            Type = type,
            Breedte = bron.Width ?? standaardAfmetingen.breedte,
            Hoogte = bron.Height ?? standaardAfmetingen.hoogte,
            Diepte = bron.Depth ?? standaardAfmetingen.diepte,
            HoogteVanVloer = bron.FloorHeight ?? standaardPlaatsing.hoogteVanVloer,
            XPositie = bron.X ?? standaardPlaatsing.xPositie
        };
    }

    private static List<Guid> MapIds(List<int>? indexen, IReadOnlyList<Guid> ids)
        => indexen?
            .Where(index => index >= 0 && index < ids.Count)
            .Select(index => ids[index])
            .ToList() ?? [];

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
                geplaatsteApparaten.Add(new Apparaat
                {
                    Id = apparaat.Id,
                    Naam = apparaat.Naam,
                    Type = apparaat.Type,
                    Breedte = apparaat.Breedte,
                    Hoogte = apparaat.Hoogte,
                    Diepte = apparaat.Diepte,
                    XPositie = plaatsing.xPositie,
                    HoogteVanVloer = plaatsing.hoogteVanVloer
                });
            }
        }

        return standaard;
    }

    private static List<MontagePlaatPositie> BouwMontagePlaatPosities(double hoogte, double wanddikte, double eersteGaatVanBoven, double gaatjesAfstand)
    {
        var posities = ScharnierBerekeningService.BerekenStandaardPosities(
            hoogte,
            wanddikte + eersteGaatVanBoven,
            gaatjesAfstand);

        return posities
            .SelectMany(afstand => new[]
            {
                new MontagePlaatPositie { AfstandVanBoven = afstand, Zijde = ScharnierZijde.Links },
                new MontagePlaatPositie { AfstandVanBoven = afstand, Zijde = ScharnierZijde.Rechts }
            })
            .OrderBy(p => p.AfstandVanBoven)
            .ThenBy(p => p.Zijde)
            .ToList();
    }

    private static KastType InferKastType(double hoogte) => hoogte switch
    {
        <= 500 => KastType.Bovenkast,
        >= 1100 => KastType.HogeKast,
        _ => KastType.Onderkast
    };
}
