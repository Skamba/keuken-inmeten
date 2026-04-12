namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public partial class KeukenStateService
{
    private const int MaxTemplates = 15;

    public bool VoegKastToe(Kast kast, Guid wandId)
    {
        SynchroniseerKast(kast, KeukenDomeinValidatieService.NormaliseerKast(kast));
        var wand = Wanden.Find(item => item.Id == wandId);
        if (wand is null || !PastKastOpWand(wand, kast))
            return false;

        if (!VoegKastToeZonderNotify(kast, wandId))
            return false;

        NotifyChanged();
        return true;
    }

    public void VerwijderAlleKasten()
    {
        foreach (var wand in Wanden)
            wand.KastIds.Clear();
        Toewijzingen.RemoveAll(t => t.KastIds.Count > 0);
        VerificatieStatussen.Clear();
        Kasten.Clear();
        NotifyChanged();
    }

    public void VerwijderKast(Guid id)
    {
        VerwijderKastZonderNotify(id);
        NotifyChanged();
    }

    private void VerwijderKastZonderNotify(Guid id)
    {
        var verwijderdeToewijzingIds = Toewijzingen
            .Where(toewijzing => toewijzing.KastIds.Contains(id))
            .Select(toewijzing => toewijzing.Id)
            .ToHashSet();

        Kasten.RemoveAll(k => k.Id == id);
        Toewijzingen.RemoveAll(t => t.KastIds.Contains(id));
        VerificatieStatussen.RemoveAll(status => verwijderdeToewijzingIds.Contains(status.ToewijzingId));
        foreach (var wand in Wanden)
            wand.KastIds.Remove(id);
    }

    public bool WerkKastBij(Kast kast)
    {
        var genormaliseerd = KeukenDomeinValidatieService.NormaliseerKast(kast);
        var index = Kasten.FindIndex(k => k.Id == genormaliseerd.Id);
        if (index < 0)
            return false;

        Kasten[index] = genormaliseerd;
        BijwerkenKastTemplate(genormaliseerd);
        NotifyChanged();
        return true;
    }

    public bool WerkKastBijOpWand(Kast kast, Guid wandId)
    {
        var doelWand = Wanden.Find(wand => wand.Id == wandId);
        if (doelWand is null)
            return false;

        var genormaliseerd = KeukenDomeinValidatieService.NormaliseerKast(kast);
        var index = Kasten.FindIndex(item => item.Id == genormaliseerd.Id);
        if (index < 0)
            return false;

        if (!PastKastOpWand(doelWand, genormaliseerd, genormaliseerd.Id))
            return false;

        Kasten[index] = genormaliseerd;
        BijwerkenKastTemplate(genormaliseerd);
        VerplaatsKastNaarWandZonderNotify(genormaliseerd.Id, wandId);
        SluitGatenNaKastWijziging(genormaliseerd, doelWand);
        NotifyChanged();
        return true;
    }

    private void SluitGatenNaKastWijziging(Kast gewijzigdeKast, KeukenWand wand)
    {
        var andereKasten = KastenVoorWand(wand.Id)
            .Where(kast => kast.Id != gewijzigdeKast.Id)
            .ToList();

        foreach (var (kastId, nieuwX, nieuwY) in WandOpstellingHelper.BepaalGatSluitingen(gewijzigdeKast, andereKasten))
        {
            var andereKast = Kasten.Find(kast => kast.Id == kastId);
            if (andereKast is null)
                continue;

            var kandidaat = IndelingFormulierHelper.KopieerKast(andereKast);
            kandidaat.XPositie = KeukenDomeinValidatieService.NormaliseerPositie(nieuwX);
            kandidaat.HoogteVanVloer = KeukenDomeinValidatieService.NormaliseerPositie(nieuwY);
            if (PastKastOpWand(wand, kandidaat, kandidaat.Id))
            {
                andereKast.XPositie = kandidaat.XPositie;
                andereKast.HoogteVanVloer = kandidaat.HoogteVanVloer;
            }
        }
    }

    public bool SluitAlleGatenOpWand(Guid wandId)
    {
        var wand = Wanden.Find(w => w.Id == wandId);
        if (wand is null)
            return false;

        var gewijzigd = false;
        var kasten = KastenVoorWand(wandId);

        bool verandering;
        do
        {
            verandering = false;
            foreach (var kast in kasten)
            {
                var anderen = kasten.Where(item => item.Id != kast.Id).ToList();
                foreach (var (kastId, nieuwX, nieuwY) in WandOpstellingHelper.BepaalGatSluitingen(kast, anderen))
                {
                    var andere = Kasten.Find(item => item.Id == kastId);
                    if (andere is null)
                        continue;

                    var kandidaat = IndelingFormulierHelper.KopieerKast(andere);
                    kandidaat.XPositie = KeukenDomeinValidatieService.NormaliseerPositie(nieuwX);
                    kandidaat.HoogteVanVloer = KeukenDomeinValidatieService.NormaliseerPositie(nieuwY);
                    if (PastKastOpWand(wand, kandidaat, kandidaat.Id))
                    {
                        andere.XPositie = kandidaat.XPositie;
                        andere.HoogteVanVloer = kandidaat.HoogteVanVloer;
                        gewijzigd = true;
                        verandering = true;
                    }
                }
            }
        }
        while (verandering);

        if (gewijzigd)
            NotifyChanged();

        return gewijzigd;
    }

    public bool VerplaatsKastNaarWand(Guid kastId, Guid wandId, int? index = null)
    {
        if (!VerplaatsKastNaarWandZonderNotify(kastId, wandId, index))
            return false;

        NotifyChanged();
        return true;
    }

    public bool VerplaatsKast(Guid id, double xPositie, double hoogteVanVloer)
    {
        var kast = Kasten.Find(item => item.Id == id);
        if (kast is null)
            return false;

        var wand = WandVoorKast(id);
        if (wand is null)
            return false;

        var genormaliseerdeX = KeukenDomeinValidatieService.NormaliseerPositie(xPositie);
        var genormaliseerdeHoogte = KeukenDomeinValidatieService.NormaliseerPositie(hoogteVanVloer);
        if (ZijnBijnaGelijk(kast.XPositie, genormaliseerdeX) && ZijnBijnaGelijk(kast.HoogteVanVloer, genormaliseerdeHoogte))
            return false;

        var kandidaat = IndelingFormulierHelper.KopieerKast(kast);
        kandidaat.XPositie = genormaliseerdeX;
        kandidaat.HoogteVanVloer = genormaliseerdeHoogte;
        if (!PastKastOpWand(wand, kandidaat, id))
            return false;

        kast.XPositie = genormaliseerdeX;
        kast.HoogteVanVloer = genormaliseerdeHoogte;
        NotifyChanged();
        return true;
    }

    public Plank? VoegPlankToe(Guid kastId, double hoogteVanBodem, Guid? plankId = null, int? index = null)
    {
        var kast = Kasten.Find(item => item.Id == kastId);
        if (kast is null)
            return null;

        var plank = new Plank
        {
            Id = plankId ?? Guid.NewGuid(),
            HoogteVanBodem = KeukenDomeinValidatieService.NormaliseerPlankHoogte(hoogteVanBodem, kast.Hoogte)
        };

        VoegPlankToeZonderNotify(kast, plank, index);
        NotifyChanged();
        return plank;
    }

    public bool VerplaatsPlank(Guid kastId, Guid plankId, double hoogteVanBodem)
    {
        var kast = Kasten.Find(item => item.Id == kastId);
        var plank = kast?.Planken.Find(item => item.Id == plankId);
        if (plank is null || kast is null)
            return false;

        var genormaliseerdeHoogte = KeukenDomeinValidatieService.NormaliseerPlankHoogte(hoogteVanBodem, kast.Hoogte);
        if (ZijnBijnaGelijk(plank.HoogteVanBodem, genormaliseerdeHoogte))
            return false;

        plank.HoogteVanBodem = genormaliseerdeHoogte;
        NotifyChanged();
        return true;
    }

    public bool VerwijderPlank(Guid kastId, Guid plankId)
    {
        var kast = Kasten.Find(item => item.Id == kastId);
        var plank = kast?.Planken.Find(item => item.Id == plankId);
        if (kast is null || plank is null)
            return false;

        kast.Planken.Remove(plank);
        NotifyChanged();
        return true;
    }

    public Plank? HerstelPlank(Guid kastId, Plank plank, int index)
    {
        var kast = Kasten.Find(item => item.Id == kastId);
        if (kast is null)
            return null;

        var kopie = new Plank
        {
            Id = plank.Id,
            HoogteVanBodem = KeukenDomeinValidatieService.NormaliseerPlankHoogte(plank.HoogteVanBodem, kast.Hoogte)
        };

        VoegPlankToeZonderNotify(kast, kopie, index);
        NotifyChanged();
        return kopie;
    }

    public bool HerstelKastMetToewijzingen(Kast kast, Guid wandId, int kastIndex, IReadOnlyList<GeindexeerdeToewijzing> toewijzingen)
    {
        var wand = Wanden.Find(item => item.Id == wandId);
        if (wand is null)
            return false;

        var genormaliseerd = KeukenDomeinValidatieService.NormaliseerKast(kast);
        if (!PastKastOpWand(wand, genormaliseerd))
            return false;

        if (!VoegKastToeZonderNotify(genormaliseerd, wandId, kastIndex))
            return false;

        foreach (var toewijzing in toewijzingen.OrderBy(item => item.Index))
            VoegToewijzingToeZonderNotify(KeukenDomeinValidatieService.NormaliseerToewijzing(toewijzing.Toewijzing), toewijzing.Index);

        NotifyChanged();
        return true;
    }

    private void BijwerkenKastTemplate(Kast kast)
    {
        var bestaand = KastTemplates.Find(t =>
            t.Breedte == kast.Breedte &&
            t.Hoogte == kast.Hoogte &&
            t.Diepte == kast.Diepte &&
            t.Wanddikte == kast.Wanddikte &&
            t.GaatjesAfstand == kast.GaatjesAfstand &&
            t.EersteGaatVanBoven == kast.EersteGaatVanBoven);

        if (bestaand is not null)
        {
            bestaand.Naam = kast.Naam;
            bestaand.LaatstGebruikt = DateTime.UtcNow;
        }
        else
        {
            KastTemplates.Add(new KastTemplate
            {
                Naam = kast.Naam,
                Type = kast.Type,
                Breedte = kast.Breedte,
                Hoogte = kast.Hoogte,
                Diepte = kast.Diepte,
                Wanddikte = kast.Wanddikte,
                GaatjesAfstand = kast.GaatjesAfstand,
                EersteGaatVanBoven = kast.EersteGaatVanBoven,
                LaatstGebruikt = DateTime.UtcNow
            });
        }

        if (KastTemplates.Count > MaxTemplates)
        {
            var oldest = KastTemplates.MinBy(t => t.LaatstGebruikt);
            if (oldest is not null)
                KastTemplates.Remove(oldest);
        }
    }
}
