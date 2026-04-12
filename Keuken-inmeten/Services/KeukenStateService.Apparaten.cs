namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public partial class KeukenStateService
{
    public bool VoegApparaatToe(Apparaat apparaat, Guid wandId)
    {
        SynchroniseerApparaat(apparaat, KeukenDomeinValidatieService.NormaliseerApparaat(apparaat));
        var wand = Wanden.Find(item => item.Id == wandId);
        if (wand is null || !PastApparaatOpWand(wand, apparaat))
            return false;

        if (!VoegApparaatToeZonderNotify(apparaat, wandId))
            return false;

        NotifyChanged();
        return true;
    }

    public void VerwijderApparaat(Guid id)
    {
        VerwijderApparaatZonderNotify(id);
        NotifyChanged();
    }

    private void VerwijderApparaatZonderNotify(Guid id)
    {
        Apparaten.RemoveAll(a => a.Id == id);
        foreach (var wand in Wanden)
            wand.ApparaatIds.Remove(id);
    }

    public bool WerkApparaatBij(Apparaat apparaat)
    {
        var genormaliseerd = KeukenDomeinValidatieService.NormaliseerApparaat(apparaat);
        var index = Apparaten.FindIndex(a => a.Id == genormaliseerd.Id);
        if (index < 0)
            return false;

        var wand = Wanden.Find(item => item.ApparaatIds.Contains(genormaliseerd.Id));
        if (wand is null || !PastApparaatOpWand(wand, genormaliseerd, genormaliseerd.Id))
            return false;

        Apparaten[index] = genormaliseerd;
        NotifyChanged();
        return true;
    }

    public bool VerplaatsApparaat(Guid id, double xPositie, double hoogteVanVloer)
    {
        var apparaat = Apparaten.Find(item => item.Id == id);
        if (apparaat is null)
            return false;

        var wand = Wanden.Find(item => item.ApparaatIds.Contains(id));
        if (wand is null)
            return false;

        var genormaliseerdeX = KeukenDomeinValidatieService.NormaliseerPositie(xPositie);
        var genormaliseerdeHoogte = KeukenDomeinValidatieService.NormaliseerPositie(hoogteVanVloer);
        if (ZijnBijnaGelijk(apparaat.XPositie, genormaliseerdeX) && ZijnBijnaGelijk(apparaat.HoogteVanVloer, genormaliseerdeHoogte))
            return false;

        var kandidaat = IndelingFormulierHelper.KopieerApparaat(apparaat);
        kandidaat.XPositie = genormaliseerdeX;
        kandidaat.HoogteVanVloer = genormaliseerdeHoogte;
        if (!PastApparaatOpWand(wand, kandidaat, id))
            return false;

        apparaat.XPositie = genormaliseerdeX;
        apparaat.HoogteVanVloer = genormaliseerdeHoogte;
        NotifyChanged();
        return true;
    }

    public bool HerstelApparaat(Apparaat apparaat, Guid wandId, int index)
    {
        var wand = Wanden.Find(item => item.Id == wandId);
        if (wand is null)
            return false;

        var genormaliseerd = KeukenDomeinValidatieService.NormaliseerApparaat(apparaat);
        if (!PastApparaatOpWand(wand, genormaliseerd))
            return false;

        if (!VoegApparaatToeZonderNotify(genormaliseerd, wandId, index))
            return false;

        NotifyChanged();
        return true;
    }

    public void StelPaneelRandSpelingIn(double waarde)
    {
        var genormaliseerd = PaneelSpelingService.NormaliseerRandSpeling(waarde);
        if (Math.Abs(PaneelRandSpeling - genormaliseerd) < 0.001)
            return;

        PaneelRandSpeling = genormaliseerd;
        NotifyChanged();
    }

    public List<Apparaat> ApparatenVoorWand(Guid wandId)
    {
        var wand = Wanden.Find(w => w.Id == wandId);
        if (wand is null)
            return [];

        return wand.ApparaatIds
            .Select(id => Apparaten.Find(a => a.Id == id))
            .Where(a => a is not null)
            .ToList()!;
    }
}
