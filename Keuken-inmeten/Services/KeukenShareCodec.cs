namespace Keuken_inmeten.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using Keuken_inmeten.Models;

public static class KeukenShareCodec
{
    private const string VersiePrefixV1 = "v1.";
    private const string VersiePrefixV2 = "v2.";

    private const double DefaultWandBreedte = 3000;
    private const double DefaultWandHoogte = 2600;
    private const double DefaultPlintHoogte = 100;
    private const double DefaultKastBreedte = 600;
    private const double DefaultKastHoogte = 720;
    private const double DefaultKastDiepte = 560;
    private const double DefaultKastWanddikte = 18;
    private const double DefaultGaatjesAfstand = 32;
    private const double DefaultEersteGaatVanBoven = 19;

    private static readonly JsonSerializerOptions LegacyJsonOpties = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions CompactJsonOpties = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string Encode(KeukenData data)
    {
        var compact = MaakCompacteData(data);
        var json = JsonSerializer.SerializeToUtf8Bytes(compact, CompactJsonOpties);
        return VersiePrefixV2 + NaarBase64Url(json);
    }

    public static bool TryDecode(string? token, out KeukenData data)
    {
        data = new KeukenData();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (token.StartsWith(VersiePrefixV2, StringComparison.Ordinal))
            return TryDecodeV2(token, out data);

        if (token.StartsWith(VersiePrefixV1, StringComparison.Ordinal))
            return TryDecodeV1(token, out data);

        return false;
    }

    private static bool TryDecodeV2(string token, out KeukenData data)
    {
        data = new KeukenData();

        try
        {
            var json = VanBase64Url(token[VersiePrefixV2.Length..]);
            var decoded = JsonSerializer.Deserialize<CompactShareData>(json, CompactJsonOpties);
            if (decoded is null)
                return false;

            data = BouwKeukenData(decoded);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDecodeV1(string token, out KeukenData data)
    {
        data = new KeukenData();

        try
        {
            var json = VanBase64Url(token[VersiePrefixV1.Length..]);
            var decoded = JsonSerializer.Deserialize<KeukenData>(json, LegacyJsonOpties);
            if (decoded is null)
                return false;

            data = NormaliseerLegacy(decoded);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static CompactShareData MaakCompacteData(KeukenData data)
    {
        var kastenOpIndex = data.Kasten
            .Select((kast, index) => (kast, index))
            .ToDictionary(item => item.kast.Id, item => item.index);
        var apparatenOpIndex = data.Apparaten
            .Select((apparaat, index) => (apparaat, index))
            .ToDictionary(item => item.apparaat.Id, item => item.index);
        var standaardKastPosities = BerekenStandaardKastPosities(data);
        var standaardApparaatPlaatsingen = BerekenStandaardApparaatPlaatsingen(data);

        return new CompactShareData
        {
            LastCupOffset = IsBijnaGelijk(data.LaatstGebruiktePotHartVanRand, ScharnierBerekeningService.CupCenterVanRand)
                ? null
                : Round1(data.LaatstGebruiktePotHartVanRand),
            Walls =
            [
                .. data.Wanden.Select(wand => new CompactWall
                {
                    Name = string.IsNullOrWhiteSpace(wand.Naam) ? null : wand.Naam,
                    Width = IsBijnaGelijk(wand.Breedte, DefaultWandBreedte) ? null : Round1(wand.Breedte),
                    Height = IsBijnaGelijk(wand.Hoogte, DefaultWandHoogte) ? null : Round1(wand.Hoogte),
                    PlinthHeight = IsBijnaGelijk(wand.PlintHoogte, DefaultPlintHoogte) ? null : Round1(wand.PlintHoogte),
                    CabinetIndexes = BouwIndexLijst(wand.KastIds, kastenOpIndex),
                    ApplianceIndexes = BouwIndexLijst(wand.ApparaatIds, apparatenOpIndex)
                })
            ],
            Cabinets =
            [
                .. data.Kasten.Select(kast => new CompactCabinet
                {
                    Name = string.IsNullOrWhiteSpace(kast.Naam) ? null : kast.Naam,
                    Width = IsBijnaGelijk(kast.Breedte, DefaultKastBreedte) ? null : Round1(kast.Breedte),
                    Height = IsBijnaGelijk(kast.Hoogte, DefaultKastHoogte) ? null : Round1(kast.Hoogte),
                    Depth = IsBijnaGelijk(kast.Diepte, DefaultKastDiepte) ? null : Round1(kast.Diepte),
                    WallThickness = IsBijnaGelijk(kast.Wanddikte, DefaultKastWanddikte) ? null : Round1(kast.Wanddikte),
                    HoleSpacing = IsBijnaGelijk(kast.GaatjesAfstand, DefaultGaatjesAfstand) ? null : Round1(kast.GaatjesAfstand),
                    FirstHoleBelowTopShelf = IsBijnaGelijk(kast.EersteGaatVanBoven, DefaultEersteGaatVanBoven) ? null : Round1(kast.EersteGaatVanBoven),
                    X = standaardKastPosities.TryGetValue(kast.Id, out var standaardKastX) && IsBijnaGelijk(kast.XPositie, standaardKastX)
                        ? null
                        : Round1(kast.XPositie),
                    FloorHeight = IsBijnaGelijk(kast.HoogteVanVloer, 0)
                        ? null
                        : Round1(kast.HoogteVanVloer),
                    ShelfHeights = kast.Planken.Count == 0
                        ? null
                        : [.. kast.Planken.Select(plank => Round1(plank.HoogteVanBodem))]
                })
            ],
            Appliances =
            [
                .. data.Apparaten.Select(apparaat =>
                {
                    var standaardAfmetingen = Apparaat.StandaardAfmetingen(apparaat.Type);
                    var standaardPlaatsing = standaardApparaatPlaatsingen.GetValueOrDefault(apparaat.Id, (0, 0));
                    return new CompactAppliance
                    {
                        Name = string.IsNullOrWhiteSpace(apparaat.Naam) ? null : apparaat.Naam,
                        Type = apparaat.Type == ApparaatType.Oven ? null : (int)apparaat.Type,
                        Width = IsBijnaGelijk(apparaat.Breedte, standaardAfmetingen.breedte) ? null : Round1(apparaat.Breedte),
                        Height = IsBijnaGelijk(apparaat.Hoogte, standaardAfmetingen.hoogte) ? null : Round1(apparaat.Hoogte),
                        Depth = IsBijnaGelijk(apparaat.Diepte, standaardAfmetingen.diepte) ? null : Round1(apparaat.Diepte),
                        X = standaardApparaatPlaatsingen.ContainsKey(apparaat.Id) && IsBijnaGelijk(apparaat.XPositie, standaardPlaatsing.xPositie)
                            ? null
                            : Round1(apparaat.XPositie),
                        FloorHeight = standaardApparaatPlaatsingen.ContainsKey(apparaat.Id) && IsBijnaGelijk(apparaat.HoogteVanVloer, standaardPlaatsing.hoogteVanVloer)
                            ? null
                            : Round1(apparaat.HoogteVanVloer)
                    };
                })
            ],
            Panels =
            [
                .. data.Toewijzingen.Select(toewijzing =>
                {
                    var panelKastIds = BouwIndexLijst(toewijzing.KastIds, kastenOpIndex) ?? [];
                    var panelKasten = toewijzing.KastIds
                        .Select(id => data.Kasten.Find(kast => kast.Id == id))
                        .Where(kast => kast is not null)
                        .Cast<Kast>()
                        .ToList();
                    var omhullende = PaneelLayoutService.BerekenOmhullende(panelKasten);
                    var isVolledigeSpan = toewijzing.XPositie is null || toewijzing.HoogteVanVloer is null ||
                        (omhullende is not null &&
                         IsBijnaGelijk(toewijzing.XPositie.Value, omhullende.XPositie) &&
                         IsBijnaGelijk(toewijzing.HoogteVanVloer.Value, omhullende.HoogteVanVloer) &&
                         IsBijnaGelijk(toewijzing.Breedte, omhullende.Breedte) &&
                         IsBijnaGelijk(toewijzing.Hoogte, omhullende.Hoogte));

                    return new CompactPanel
                    {
                        CabinetIndexes = panelKastIds,
                        Type = toewijzing.Type == PaneelType.Deur ? null : (int)toewijzing.Type,
                        HingeSide = toewijzing.ScharnierZijde == ScharnierZijde.Links ? null : (int)toewijzing.ScharnierZijde,
                        CupOffset = toewijzing.Type == PaneelType.Deur &&
                                    !IsBijnaGelijk(toewijzing.PotHartVanRand, ScharnierBerekeningService.CupCenterVanRand)
                            ? Round1(toewijzing.PotHartVanRand)
                            : null,
                        Width = isVolledigeSpan ? null : Round1(toewijzing.Breedte),
                        Height = isVolledigeSpan ? null : Round1(toewijzing.Hoogte),
                        X = isVolledigeSpan ? null : Round1(toewijzing.XPositie ?? 0),
                        FloorHeight = isVolledigeSpan ? null : Round1(toewijzing.HoogteVanVloer ?? 0)
                    };
                })
            ]
        };
    }

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

    private static List<int>? BouwIndexLijst(IEnumerable<Guid> ids, IReadOnlyDictionary<Guid, int> indexen)
    {
        var result = ids
            .Where(indexen.ContainsKey)
            .Select(id => indexen[id])
            .ToList();

        return result.Count == 0 ? null : result;
    }

    private static KeukenData NormaliseerLegacy(KeukenData data) => new()
    {
        Wanden = [.. data.Wanden],
        Kasten = [.. data.Kasten],
        Apparaten = [.. data.Apparaten],
        Toewijzingen = [.. data.Toewijzingen],
        KastTemplates = [],
        LaatstGebruiktePotHartVanRand = data.LaatstGebruiktePotHartVanRand
    };

    private static KeukenData BouwKeukenData(CompactShareData data)
    {
        var wallIds = data.Walls.Select(_ => Guid.NewGuid()).ToList();
        var cabinetIds = data.Cabinets.Select(_ => Guid.NewGuid()).ToList();
        var applianceIds = data.Appliances.Select(_ => Guid.NewGuid()).ToList();
        var defaultCabinetX = BerekenDecodeStandaardKastPosities(data);
        var defaultAppliancePlaatsingen = BerekenDecodeStandaardApparaatPlaatsingen(data, defaultCabinetX);

        var wanden = data.Walls.Select((wand, index) => new KeukenWand
        {
            Id = wallIds[index],
            Naam = wand.Name ?? "",
            Breedte = wand.Width ?? DefaultWandBreedte,
            Hoogte = wand.Height ?? DefaultWandHoogte,
            PlintHoogte = wand.PlinthHeight ?? DefaultPlintHoogte,
            KastIds = MapIds(wand.CabinetIndexes, cabinetIds),
            ApparaatIds = MapIds(wand.ApplianceIndexes, applianceIds)
        }).ToList();

        var kasten = data.Cabinets.Select((kast, index) => BouwKast(kast, cabinetIds[index], defaultCabinetX.GetValueOrDefault(index, 0))).ToList();
        var apparaten = data.Appliances.Select((apparaat, index) =>
        {
            var standaardPlaatsing = defaultAppliancePlaatsingen.GetValueOrDefault(index, (0, 0));
            return BouwApparaat(apparaat, applianceIds[index], standaardPlaatsing);
        }).ToList();
        var kastenOpId = kasten.ToDictionary(kast => kast.Id);

        var toewijzingen = data.Panels.Select(panel =>
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
                PotHartVanRand = panel.CupOffset ?? ScharnierBerekeningService.CupCenterVanRand,
                Breedte = Round1(panel.Width ?? omhullende?.Breedte ?? 0),
                Hoogte = Round1(panel.Height ?? omhullende?.Hoogte ?? 0),
                XPositie = heeftExplicietePlaatsing ? Round1(panel.X ?? omhullende?.XPositie ?? 0) : null,
                HoogteVanVloer = heeftExplicietePlaatsing ? Round1(panel.FloorHeight ?? omhullende?.HoogteVanVloer ?? 0) : null
            };
        }).ToList();

        return new KeukenData
        {
            Wanden = wanden,
            Kasten = kasten,
            Apparaten = apparaten,
            Toewijzingen = toewijzingen,
            KastTemplates = [],
            LaatstGebruiktePotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(data.LastCupOffset ?? ScharnierBerekeningService.CupCenterVanRand)
        };
    }

    private static Kast BouwKast(CompactCabinet bron, Guid id, double standaardX)
    {
        var hoogte = bron.Height ?? DefaultKastHoogte;
        var wanddikte = bron.WallThickness ?? DefaultKastWanddikte;
        var eersteGaatVanBoven = bron.FirstHoleBelowTopShelf ?? DefaultEersteGaatVanBoven;
        var gaatjesAfstand = bron.HoleSpacing ?? DefaultGaatjesAfstand;

        return new Kast
        {
            Id = id,
            Naam = bron.Name ?? "",
            Type = InferKastType(hoogte),
            Breedte = bron.Width ?? DefaultKastBreedte,
            Hoogte = hoogte,
            Diepte = bron.Depth ?? DefaultKastDiepte,
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
        var standaard = new Dictionary<int, double>();

        foreach (var wand in data.Walls)
        {
            double lopendeX = 0;
            foreach (var index in wand.CabinetIndexes ?? [])
            {
                if (index < 0 || index >= data.Cabinets.Count)
                    continue;

                standaard[index] = Round1(lopendeX);
                lopendeX += data.Cabinets[index].Width ?? DefaultKastBreedte;
            }
        }

        return standaard;
    }

    private static Dictionary<int, (double xPositie, double hoogteVanVloer)> BerekenDecodeStandaardApparaatPlaatsingen(
        CompactShareData data,
        IReadOnlyDictionary<int, double> defaultCabinetX)
    {
        var standaard = new Dictionary<int, (double xPositie, double hoogteVanVloer)>();

        foreach (var wand in data.Walls)
        {
            var wandModel = new KeukenWand
            {
                Id = Guid.NewGuid(),
                Naam = wand.Name ?? "",
                Breedte = wand.Width ?? DefaultWandBreedte,
                Hoogte = wand.Height ?? DefaultWandHoogte,
                PlintHoogte = wand.PlinthHeight ?? DefaultPlintHoogte
            };
            var wandKasten = (wand.CabinetIndexes ?? [])
                .Where(index => index >= 0 && index < data.Cabinets.Count)
                .Select(index => BouwKast(data.Cabinets[index], Guid.NewGuid(), defaultCabinetX.GetValueOrDefault(index, 0)))
                .ToList();
            var geplaatsteApparaten = new List<Apparaat>();

            foreach (var index in wand.ApplianceIndexes ?? [])
            {
                if (index < 0 || index >= data.Appliances.Count)
                    continue;

                var type = data.Appliances[index].Type is int typeNummer ? (ApparaatType)typeNummer : ApparaatType.Oven;
                var standaardAfmetingen = Apparaat.StandaardAfmetingen(type);
                var apparaat = new Apparaat
                {
                    Id = Guid.NewGuid(),
                    Naam = data.Appliances[index].Name ?? "",
                    Type = type,
                    Breedte = data.Appliances[index].Width ?? standaardAfmetingen.breedte,
                    Hoogte = data.Appliances[index].Height ?? standaardAfmetingen.hoogte,
                    Diepte = data.Appliances[index].Depth ?? standaardAfmetingen.diepte
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

    private static double Round1(double waarde) => Math.Round(waarde, 1);

    private static bool IsBijnaGelijk(double links, double rechts) => Math.Abs(links - rechts) < 0.001;

    private static string NaarBase64Url(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] VanBase64Url(string base64Url)
    {
        var padded = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        var remainder = padded.Length % 4;
        if (remainder > 0)
            padded = padded.PadRight(padded.Length + (4 - remainder), '=');

        return Convert.FromBase64String(padded);
    }

    private sealed class CompactShareData
    {
        [JsonPropertyName("w")]
        public List<CompactWall> Walls { get; set; } = [];

        [JsonPropertyName("k")]
        public List<CompactCabinet> Cabinets { get; set; } = [];

        [JsonPropertyName("a")]
        public List<CompactAppliance> Appliances { get; set; } = [];

        [JsonPropertyName("t")]
        public List<CompactPanel> Panels { get; set; } = [];

        [JsonPropertyName("p")]
        public double? LastCupOffset { get; set; }
    }

    private sealed class CompactWall
    {
        [JsonPropertyName("n")]
        public string? Name { get; set; }

        [JsonPropertyName("b")]
        public double? Width { get; set; }

        [JsonPropertyName("h")]
        public double? Height { get; set; }

        [JsonPropertyName("p")]
        public double? PlinthHeight { get; set; }

        [JsonPropertyName("k")]
        public List<int>? CabinetIndexes { get; set; }

        [JsonPropertyName("a")]
        public List<int>? ApplianceIndexes { get; set; }
    }

    private sealed class CompactCabinet
    {
        [JsonPropertyName("n")]
        public string? Name { get; set; }

        [JsonPropertyName("b")]
        public double? Width { get; set; }

        [JsonPropertyName("h")]
        public double? Height { get; set; }

        [JsonPropertyName("d")]
        public double? Depth { get; set; }

        [JsonPropertyName("w")]
        public double? WallThickness { get; set; }

        [JsonPropertyName("g")]
        public double? HoleSpacing { get; set; }

        [JsonPropertyName("e")]
        public double? FirstHoleBelowTopShelf { get; set; }

        [JsonPropertyName("x")]
        public double? X { get; set; }

        [JsonPropertyName("y")]
        public double? FloorHeight { get; set; }

        [JsonPropertyName("p")]
        public List<double>? ShelfHeights { get; set; }
    }

    private sealed class CompactAppliance
    {
        [JsonPropertyName("n")]
        public string? Name { get; set; }

        [JsonPropertyName("t")]
        public int? Type { get; set; }

        [JsonPropertyName("b")]
        public double? Width { get; set; }

        [JsonPropertyName("h")]
        public double? Height { get; set; }

        [JsonPropertyName("d")]
        public double? Depth { get; set; }

        [JsonPropertyName("x")]
        public double? X { get; set; }

        [JsonPropertyName("y")]
        public double? FloorHeight { get; set; }
    }

    private sealed class CompactPanel
    {
        [JsonPropertyName("c")]
        public List<int>? CabinetIndexes { get; set; }

        [JsonPropertyName("t")]
        public int? Type { get; set; }

        [JsonPropertyName("s")]
        public int? HingeSide { get; set; }

        [JsonPropertyName("p")]
        public double? CupOffset { get; set; }

        [JsonPropertyName("b")]
        public double? Width { get; set; }

        [JsonPropertyName("h")]
        public double? Height { get; set; }

        [JsonPropertyName("x")]
        public double? X { get; set; }

        [JsonPropertyName("y")]
        public double? FloorHeight { get; set; }
    }
}
