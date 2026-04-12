namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static partial class KeukenShareCodec
{
    private static CompactShareData MaakCompacteData(KeukenData data)
    {
        var verificatieStatusOpToewijzingId = data.VerificatieStatussen
            .GroupBy(status => status.ToewijzingId)
            .ToDictionary(groep => groep.Key, groep => groep.Last());
        var kastenOpIndex = data.Kasten
            .Select((kast, index) => (kast, index))
            .ToDictionary(item => item.kast.Id, item => item.index);
        var apparatenOpIndex = data.Apparaten
            .Select((apparaat, index) => (apparaat, index))
            .ToDictionary(item => item.apparaat.Id, item => item.index);
        var standaardKastPosities = BerekenStandaardKastPosities(data);
        var standaardApparaatPlaatsingen = BerekenStandaardApparaatPlaatsingen(data);
        var walls = data.Wanden.Select(wand => new CompactWall
        {
            Name = string.IsNullOrWhiteSpace(wand.Naam) ? null : wand.Naam,
            Width = IsBijnaGelijk(wand.Breedte, DefaultWand.Breedte) ? null : Round1(wand.Breedte),
            Height = IsBijnaGelijk(wand.Hoogte, DefaultWand.Hoogte) ? null : Round1(wand.Hoogte),
            PlinthHeight = IsBijnaGelijk(wand.PlintHoogte, DefaultWand.PlintHoogte) ? null : Round1(wand.PlintHoogte),
            CabinetIndexes = BouwIndexLijst(wand.KastIds, kastenOpIndex),
            ApplianceIndexes = BouwIndexLijst(wand.ApparaatIds, apparatenOpIndex)
        }).ToList();
        var cabinets = data.Kasten.Select(kast => new CompactCabinet
        {
            Name = string.IsNullOrWhiteSpace(kast.Naam) ? null : kast.Naam,
            Width = IsBijnaGelijk(kast.Breedte, DefaultKast.Breedte) ? null : Round1(kast.Breedte),
            Height = IsBijnaGelijk(kast.Hoogte, DefaultKast.Hoogte) ? null : Round1(kast.Hoogte),
            Depth = IsBijnaGelijk(kast.Diepte, DefaultKast.Diepte) ? null : Round1(kast.Diepte),
            WallThickness = IsBijnaGelijk(kast.Wanddikte, DefaultKast.Wanddikte) ? null : Round1(kast.Wanddikte),
            HoleSpacing = IsBijnaGelijk(kast.GaatjesAfstand, DefaultKast.GaatjesAfstand) ? null : Round1(kast.GaatjesAfstand),
            FirstHoleBelowTopShelf = IsBijnaGelijk(kast.EersteGaatVanBoven, DefaultKast.EersteGaatVanBoven) ? null : Round1(kast.EersteGaatVanBoven),
            X = standaardKastPosities.TryGetValue(kast.Id, out var standaardKastX) && IsBijnaGelijk(kast.XPositie, standaardKastX)
                ? null
                : Round1(kast.XPositie),
            FloorHeight = IsBijnaGelijk(kast.HoogteVanVloer, 0)
                ? null
                : Round1(kast.HoogteVanVloer),
            ShelfHeights = kast.Planken.Count == 0
                ? null
                : [.. kast.Planken.Select(plank => Round1(plank.HoogteVanBodem))]
        }).ToList();
        var appliances = data.Apparaten.Select(apparaat =>
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
        }).ToList();
        var panels = data.Toewijzingen.Select(toewijzing =>
        {
            var panelKastIds = BouwIndexLijst(toewijzing.KastIds, kastenOpIndex) ?? [];
            var panelKasten = toewijzing.KastIds
                .Select(id => data.Kasten.Find(kast => kast.Id == id))
                .Where(kast => kast is not null)
                .Cast<Kast>()
                .ToList();
            var omhullende = PaneelLayoutService.BerekenOmhullende(panelKasten);
            var standaardBreedte = omhullende?.Breedte;
            var standaardHoogte = omhullende?.Hoogte;
            var standaardX = omhullende?.XPositie;
            var standaardVloer = omhullende?.HoogteVanVloer;

            return new CompactPanel
            {
                CabinetIndexes = panelKastIds,
                Type = toewijzing.Type == PaneelType.Deur ? null : (int)toewijzing.Type,
                HingeSide = toewijzing.ScharnierZijde == ScharnierZijde.Links ? null : (int)toewijzing.ScharnierZijde,
                CupOffset = toewijzing.Type == PaneelType.Deur &&
                            !IsBijnaGelijk(toewijzing.PotHartVanRand, KeukenDomeinDefaults.ProjectDefaults.LaatstGebruiktePotHartVanRand)
                    ? Round1(toewijzing.PotHartVanRand)
                    : null,
                Width = standaardBreedte is double breedte && IsBijnaGelijk(toewijzing.Breedte, breedte)
                    ? null
                    : Round1(toewijzing.Breedte),
                Height = standaardHoogte is double hoogte && IsBijnaGelijk(toewijzing.Hoogte, hoogte)
                    ? null
                    : Round1(toewijzing.Hoogte),
                X = toewijzing.XPositie is double xPositie
                    ? standaardX is double xStandaard && IsBijnaGelijk(xPositie, xStandaard)
                        ? null
                        : Round1(xPositie)
                    : null,
                FloorHeight = toewijzing.HoogteVanVloer is double hoogteVanVloer
                    ? standaardVloer is double vloerStandaard && IsBijnaGelijk(hoogteVanVloer, vloerStandaard)
                        ? null
                        : Round1(hoogteVanVloer)
                    : null,
                VerificationStatus = EncodeVerificatieStatus(verificatieStatusOpToewijzingId.GetValueOrDefault(toewijzing.Id))
            };
        }).ToList();

        return new CompactShareData
        {
            SchemaVersion = KeukenDataMigratieService.HuidigeSchemaVersie,
            LastCupOffset = IsBijnaGelijk(data.LaatstGebruiktePotHartVanRand, KeukenDomeinDefaults.ProjectDefaults.LaatstGebruiktePotHartVanRand)
                ? null
                : Round1(data.LaatstGebruiktePotHartVanRand),
            PanelEdgeClearance = IsBijnaGelijk(data.PaneelRandSpeling, KeukenDomeinDefaults.ProjectDefaults.PaneelRandSpeling)
                ? null
                : Round1(data.PaneelRandSpeling),
            Walls = walls.Count == 0 ? null : walls,
            Cabinets = cabinets.Count == 0 ? null : cabinets,
            Appliances = appliances.Count == 0 ? null : appliances,
            Panels = panels.Count == 0 ? null : panels
        };
    }

    private static int? EncodeVerificatieStatus(PaneelVerificatieStatus? status)
    {
        if (status is null)
            return null;

        var bits = 0;
        if (status.MatenOk)
            bits |= 1;
        if (status.ScharnierPositiesOk)
            bits |= 2;

        return bits == 0 ? null : bits;
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
}
