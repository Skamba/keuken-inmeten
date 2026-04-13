namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public partial class KeukenStateService
{
    private bool VoegKastToeZonderNotify(Kast kast, Guid wandId, int? index = null)
    {
        var wand = VindWand(wandId);
        if (wand is null)
            return false;

        Kasten.Add(kast);
        PlaatsIdInVolgorde(wand.KastIds, kast.Id, index);
        BijwerkenKastTemplate(kast);
        return true;
    }

    private static void VoegPlankToeZonderNotify(Kast kast, Plank plank, int? index = null)
    {
        if (index is int insertIndex)
            kast.Planken.Insert(Math.Clamp(insertIndex, 0, kast.Planken.Count), plank);
        else
            kast.Planken.Add(plank);
    }

    private bool VoegApparaatToeZonderNotify(Apparaat apparaat, Guid wandId, int? index = null)
    {
        var wand = VindWand(wandId);
        if (wand is null)
            return false;

        Apparaten.Add(apparaat);
        PlaatsIdInVolgorde(wand.ApparaatIds, apparaat.Id, index);
        return true;
    }

    private void VoegToewijzingToeZonderNotify(PaneelToewijzing toewijzing, int? index = null)
    {
        if (toewijzing.Type == PaneelType.Deur)
            LaatstGebruiktePotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(toewijzing.PotHartVanRand);

        if (index is int insertIndex)
            Toewijzingen.Insert(Math.Clamp(insertIndex, 0, Toewijzingen.Count), toewijzing);
        else
            Toewijzingen.Add(toewijzing);
    }

    private static bool ZijnBijnaGelijk(double links, double rechts)
        => Math.Abs(links - rechts) < 0.001;

    private KeukenWand? VindWand(Guid wandId)
        => Wanden.Find(item => item.Id == wandId);

    private KeukenWand? VindWandVoorKast(Guid kastId)
        => Wanden.Find(item => item.KastIds.Contains(kastId));

    private KeukenWand? VindWandVoorApparaat(Guid apparaatId)
        => Wanden.Find(item => item.ApparaatIds.Contains(apparaatId));

    private Kast? VindKast(Guid kastId)
        => Kasten.Find(item => item.Id == kastId);

    private Apparaat? VindApparaat(Guid apparaatId)
        => Apparaten.Find(item => item.Id == apparaatId);

    private static List<T> ZoekEntiteitenOpVolgorde<T>(
        IEnumerable<Guid> ids,
        IEnumerable<T> bron,
        Func<T, Guid> idSelector)
        where T : class
    {
        var lookup = bron.ToDictionary(idSelector);

        return ids
            .Select(lookup.GetValueOrDefault)
            .Where(item => item is not null)
            .Cast<T>()
            .ToList();
    }

    private static void PlaatsIdInVolgorde(List<Guid> ids, Guid id, int? index = null)
    {
        ids.Remove(id);

        if (index is int insertIndex)
            ids.Insert(Math.Clamp(insertIndex, 0, ids.Count), id);
        else
            ids.Add(id);
    }

    private bool PastIndelingOpWand(KeukenWand wand)
    {
        var wandKasten = KastenVoorWand(wand.Id);
        foreach (var kast in wandKasten)
        {
            if (!PastKastOpWand(wand, kast, kast.Id))
                return false;
        }

        var wandApparaten = ApparatenVoorWand(wand.Id);
        foreach (var apparaat in wandApparaten)
        {
            if (!PastApparaatOpWand(wand, apparaat, apparaat.Id))
                return false;
        }

        return true;
    }

    private bool PastKastOpWand(KeukenWand wand, Kast kast, Guid? uitsluitenKastId = null)
        => KastPlaatsingService.IsVrijePlaatsing(
            wand,
            KastenVoorWand(wand.Id),
            kast,
            kast.XPositie,
            kast.HoogteVanVloer,
            uitsluitenKastId);

    private bool PastApparaatOpWand(KeukenWand wand, Apparaat apparaat, Guid? uitsluitenApparaatId = null)
    {
        var maxX = wand.Breedte - apparaat.Breedte;
        var maxY = wand.Hoogte - apparaat.Hoogte;
        if (maxX < -0.001 || maxY < -0.001)
            return false;

        if (apparaat.XPositie > Math.Max(0, maxX) + 0.001 || apparaat.HoogteVanVloer > Math.Max(0, maxY) + 0.001)
            return false;

        foreach (var kast in KastenVoorWand(wand.Id))
        {
            if (ApparaatLayoutService.HeeftOverlap(apparaat, kast))
                return false;
        }

        foreach (var bestaandApparaat in ApparatenVoorWand(wand.Id).Where(item => item.Id != uitsluitenApparaatId))
        {
            if (ApparaatLayoutService.HeeftOverlap(apparaat, bestaandApparaat))
                return false;
        }

        return true;
    }

    private bool VerplaatsKastNaarWandZonderNotify(Guid kastId, Guid wandId, int? index = null)
    {
        var doelWand = VindWand(wandId);
        if (doelWand is null)
            return false;

        var huidigeWand = VindWandVoorKast(kastId);
        if (huidigeWand?.Id == wandId)
        {
            if (index is not int insertIndex)
                return true;

            PlaatsIdInVolgorde(huidigeWand.KastIds, kastId, insertIndex);
            return true;
        }

        huidigeWand?.KastIds.Remove(kastId);
        PlaatsIdInVolgorde(doelWand.KastIds, kastId, index);
        return true;
    }

    private static void SynchroniseerWand(KeukenWand doel, KeukenWand bron)
    {
        doel.Id = bron.Id;
        doel.Naam = bron.Naam;
        doel.Breedte = bron.Breedte;
        doel.Hoogte = bron.Hoogte;
        doel.PlintHoogte = bron.PlintHoogte;
        doel.KastIds = bron.KastIds;
        doel.ApparaatIds = bron.ApparaatIds;
    }

    private static void SynchroniseerKast(Kast doel, Kast bron)
    {
        doel.Id = bron.Id;
        doel.Naam = bron.Naam;
        doel.Type = bron.Type;
        doel.Breedte = bron.Breedte;
        doel.Hoogte = bron.Hoogte;
        doel.Diepte = bron.Diepte;
        doel.Wanddikte = bron.Wanddikte;
        doel.GaatjesAfstand = bron.GaatjesAfstand;
        doel.EersteGaatVanBoven = bron.EersteGaatVanBoven;
        doel.HoogteVanVloer = bron.HoogteVanVloer;
        doel.XPositie = bron.XPositie;
        doel.MontagePlaatPosities = bron.MontagePlaatPosities;
        doel.Planken = bron.Planken;
    }

    private static void SynchroniseerApparaat(Apparaat doel, Apparaat bron)
    {
        doel.Id = bron.Id;
        doel.Naam = bron.Naam;
        doel.Type = bron.Type;
        doel.Breedte = bron.Breedte;
        doel.Hoogte = bron.Hoogte;
        doel.Diepte = bron.Diepte;
        doel.HoogteVanVloer = bron.HoogteVanVloer;
        doel.XPositie = bron.XPositie;
    }
}

public sealed record GeindexeerdeToewijzing(PaneelToewijzing Toewijzing, int Index);
