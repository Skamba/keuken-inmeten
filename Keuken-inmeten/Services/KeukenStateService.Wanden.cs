namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public partial class KeukenStateService
{
    public void VoegWandToe(KeukenWand wand)
    {
        SynchroniseerWand(wand, KeukenDomeinValidatieService.NormaliseerWand(wand));
        Wanden.Add(wand);
        NotifyChanged();
    }

    public void VerwijderWand(Guid id)
    {
        var wand = VindWand(id);
        if (wand is not null)
        {
            foreach (var kastId in wand.KastIds.ToList())
                VerwijderKastZonderNotify(kastId);
            foreach (var apparaatId in wand.ApparaatIds.ToList())
                VerwijderApparaatZonderNotify(apparaatId);
            Wanden.RemoveAll(w => w.Id == id);
        }

        NotifyChanged();
    }

    public bool WerkWandBij(KeukenWand wand)
    {
        var index = Wanden.FindIndex(w => w.Id == wand.Id);
        if (index < 0)
            return false;

        Wanden[index] = KeukenDomeinValidatieService.NormaliseerWand(wand);
        NotifyChanged();
        return true;
    }

    public bool HernoemWand(Guid id, string naam)
    {
        var wand = VindWand(id);
        var opgeschoondeNaam = naam.Trim();
        if (wand is null || string.IsNullOrWhiteSpace(opgeschoondeNaam) || wand.Naam == opgeschoondeNaam)
            return false;

        wand.Naam = opgeschoondeNaam;
        NotifyChanged();
        return true;
    }

    public bool WerkWandAfmetingenBij(Guid id, double breedte, double hoogte, double plintHoogte)
    {
        if (breedte <= 0 || hoogte <= 0 || plintHoogte < 0)
            return false;

        var wand = VindWand(id);
        if (wand is null)
            return false;

        var gewijzigdeBreedte = !ZijnBijnaGelijk(wand.Breedte, breedte);
        var gewijzigdeHoogte = !ZijnBijnaGelijk(wand.Hoogte, hoogte);
        var gewijzigdePlint = !ZijnBijnaGelijk(wand.PlintHoogte, plintHoogte);
        var gewijzigd = gewijzigdeBreedte || gewijzigdeHoogte || gewijzigdePlint;
        if (!gewijzigd)
            return false;

        var kandidaatWand = new KeukenWand
        {
            Id = wand.Id,
            Naam = wand.Naam,
            Breedte = breedte,
            Hoogte = hoogte,
            PlintHoogte = plintHoogte,
            KastIds = [.. wand.KastIds],
            ApparaatIds = [.. wand.ApparaatIds]
        };
        if (!PastIndelingOpWand(kandidaatWand))
            return false;

        wand.Breedte = breedte;
        wand.Hoogte = hoogte;
        wand.PlintHoogte = plintHoogte;
        NotifyChanged();
        return true;
    }

    public List<Kast> KastenVoorWand(Guid wandId)
    {
        var wand = VindWand(wandId);
        if (wand is null)
            return [];

        return ZoekKasten(wand.KastIds);
    }

    public KeukenWand? ZoekWand(Guid wandId)
        => VindWand(wandId);

    public List<Kast> ZoekKasten(IEnumerable<Guid> kastIds)
        => ZoekEntiteitenOpVolgorde(kastIds, Kasten, item => item.Id);

    public KeukenWand? WandVoorKast(Guid kastId)
        => VindWandVoorKast(kastId);

    public string WandNaamVoorKasten(IEnumerable<Guid> kastIds, string geenWandLabel = "—")
        => kastIds
            .Select(id => WandVoorKast(id)?.Naam)
            .FirstOrDefault(naam => !string.IsNullOrWhiteSpace(naam)) ?? geenWandLabel;

    public void VerplaatsKastInWand(Guid wandId, int vanIndex, int naarIndex)
    {
        var wand = VindWand(wandId);
        if (wand is null)
            return;
        if (vanIndex < 0 || vanIndex >= wand.KastIds.Count)
            return;
        if (naarIndex < 0 || naarIndex >= wand.KastIds.Count)
            return;

        var kastId = wand.KastIds[vanIndex];
        wand.KastIds.RemoveAt(vanIndex);
        wand.KastIds.Insert(naarIndex, kastId);
        NotifyChanged();
    }
}
