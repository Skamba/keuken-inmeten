namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public class KeukenStateService
{
    public List<KeukenWand> Wanden { get; } = [];
    public List<Kast> Kasten { get; } = [];
    public List<Apparaat> Apparaten { get; } = [];
    public List<PaneelToewijzing> Toewijzingen { get; } = [];
    public List<KastTemplate> KastTemplates { get; } = [];
    public double LaatstGebruiktePotHartVanRand { get; private set; } = ScharnierBerekeningService.CupCenterVanRand;
    public double PaneelRandSpeling { get; private set; } = PaneelSpelingService.DefaultRandSpeling;

    /// <summary>Fires after any state mutation so subscribers can persist the data.</summary>
    public event Action? OnStateChanged;

    private void NotifyChanged() => OnStateChanged?.Invoke();

    // --- Persistentie helpers ---

    public KeukenData Exporteren() => new()
    {
        Wanden = [.. Wanden],
        Kasten = [.. Kasten],
        Apparaten = [.. Apparaten],
        Toewijzingen = [.. Toewijzingen],
        KastTemplates = [.. KastTemplates],
        LaatstGebruiktePotHartVanRand = LaatstGebruiktePotHartVanRand,
        PaneelRandSpeling = PaneelRandSpeling
    };

    public void Laden(KeukenData data)
    {
        var genormaliseerd = KeukenDomeinValidatieService.NormaliseerData(data);
        Wanden.Clear();
        Kasten.Clear();
        Apparaten.Clear();
        Toewijzingen.Clear();
        KastTemplates.Clear();

        Wanden.AddRange(genormaliseerd.Wanden);
        Kasten.AddRange(genormaliseerd.Kasten);
        Apparaten.AddRange(genormaliseerd.Apparaten);
        Toewijzingen.AddRange(genormaliseerd.Toewijzingen);
        KastTemplates.AddRange(genormaliseerd.KastTemplates);
        LaatstGebruiktePotHartVanRand = genormaliseerd.LaatstGebruiktePotHartVanRand;
        PaneelRandSpeling = genormaliseerd.PaneelRandSpeling;
    }

    public bool HeeftProjectInhoud()
        => Wanden.Count > 0 ||
           Kasten.Count > 0 ||
           Apparaten.Count > 0 ||
           Toewijzingen.Count > 0 ||
           KastTemplates.Count > 0 ||
           !ZijnBijnaGelijk(LaatstGebruiktePotHartVanRand, ScharnierBerekeningService.CupCenterVanRand) ||
           !ZijnBijnaGelijk(PaneelRandSpeling, PaneelSpelingService.DefaultRandSpeling);

    public void Importeer(KeukenData data)
    {
        Laden(data);
        NotifyChanged();
    }

    public void VerwijderAlles()
        => Importeer(new KeukenData());

    // --- Wanden ---

    public void VoegWandToe(KeukenWand wand)
    {
        SynchroniseerWand(wand, KeukenDomeinValidatieService.NormaliseerWand(wand));
        Wanden.Add(wand);
        NotifyChanged();
    }

    public void VerwijderWand(Guid id)
    {
        var wand = Wanden.Find(w => w.Id == id);
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
        var wand = Wanden.Find(item => item.Id == id);
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

        var wand = Wanden.Find(item => item.Id == id);
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
        var wand = Wanden.Find(w => w.Id == wandId);
        if (wand is null) return [];
        return ZoekKasten(wand.KastIds);
    }

    /// <summary>
    /// Zoekt kasten op basis van een lijst met IDs.
    /// Behoudt de volgorde van de IDs en negeert ontbrekende kasten.
    /// </summary>
    public List<Kast> ZoekKasten(List<Guid> kastIds)
        => kastIds
            .Select(id => Kasten.Find(k => k.Id == id))
            .Where(k => k is not null)
            .ToList()!;

    /// <summary>
    /// Zoekt de wand die een bepaalde kast bevat.
    /// </summary>
    public KeukenWand? WandVoorKast(Guid kastId)
        => Wanden.Find(w => w.KastIds.Contains(kastId));

    /// <summary>
    /// Zoekt de wandnaam voor een lijst met kastIDs (neemt de eerste match).
    /// </summary>
    public string WandNaamVoorKasten(IEnumerable<Guid> kastIds, string geenWandLabel = "—")
        => kastIds
            .Select(id => WandVoorKast(id)?.Naam)
            .FirstOrDefault(naam => !string.IsNullOrWhiteSpace(naam)) ?? geenWandLabel;

    public void VerplaatsKastInWand(Guid wandId, int vanIndex, int naarIndex)
    {
        var wand = Wanden.Find(w => w.Id == wandId);
        if (wand is null) return;
        if (vanIndex < 0 || vanIndex >= wand.KastIds.Count) return;
        if (naarIndex < 0 || naarIndex >= wand.KastIds.Count) return;

        var kastId = wand.KastIds[vanIndex];
        wand.KastIds.RemoveAt(vanIndex);
        wand.KastIds.Insert(naarIndex, kastId);
        NotifyChanged();
    }

    // --- Kasten ---

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
        Kasten.RemoveAll(k => k.Id == id);
        Toewijzingen.RemoveAll(t => t.KastIds.Contains(id));
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
        NotifyChanged();
        return true;
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

    // --- Kast templates (eerder gebruikte kasten) ---

    private const int MaxTemplates = 15;

    private void BijwerkenKastTemplate(Kast kast)
    {
        // Match on dimensions; update name + timestamp if found, otherwise add new
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

        // Keep only the most recently used templates
        if (KastTemplates.Count > MaxTemplates)
        {
            var oldest = KastTemplates.MinBy(t => t.LaatstGebruikt);
            if (oldest is not null)
                KastTemplates.Remove(oldest);
        }
    }

    // --- Apparaten ---

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
        if (wand is null) return [];
        return wand.ApparaatIds
            .Select(id => Apparaten.Find(a => a.Id == id))
            .Where(a => a is not null)
            .ToList()!;
    }

    public void VoegToewijzingToe(PaneelToewijzing toewijzing)
    {
        VoegToewijzingToeZonderNotify(KeukenDomeinValidatieService.NormaliseerToewijzing(toewijzing));
        NotifyChanged();
    }

    public void VoegToewijzingenToe(IEnumerable<PaneelToewijzing> toewijzingen)
    {
        var genormaliseerd = toewijzingen
            .Select(KeukenDomeinValidatieService.NormaliseerToewijzing)
            .ToList();
        if (genormaliseerd.Count == 0)
            return;

        foreach (var toewijzing in genormaliseerd)
            VoegToewijzingToeZonderNotify(toewijzing);

        NotifyChanged();
    }

    public void WerkToewijzingBij(PaneelToewijzing toewijzing)
    {
        var genormaliseerd = KeukenDomeinValidatieService.NormaliseerToewijzing(toewijzing);
        var index = Toewijzingen.FindIndex(t => t.Id == genormaliseerd.Id);
        if (index < 0)
            return;

        if (genormaliseerd.Type == PaneelType.Deur)
            LaatstGebruiktePotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(genormaliseerd.PotHartVanRand);

        Toewijzingen[index] = genormaliseerd;
        NotifyChanged();
    }

    public void VerwijderToewijzing(Guid id)
    {
        Toewijzingen.RemoveAll(t => t.Id == id);
        NotifyChanged();
    }

    public bool HerstelToewijzing(PaneelToewijzing toewijzing, int index)
    {
        VoegToewijzingToeZonderNotify(KeukenDomeinValidatieService.NormaliseerToewijzing(toewijzing), index);
        NotifyChanged();
        return true;
    }

    public List<PaneelResultaat> BerekenResultaten()
    {
        var resultaten = new List<PaneelResultaat>();

        foreach (var toewijzing in Toewijzingen)
        {
            var kasten = ZoekKasten(toewijzing.KastIds);
            if (kasten.Count > 0)
            {
                var maatInfo = BerekenPaneelMaatInfo(toewijzing, kasten);
                resultaten.Add(ScharnierBerekeningService.BerekenPaneel(toewijzing, kasten, maatInfo));
            }
        }

        return resultaten;
    }

    public PaneelMaatInfo? BerekenPaneelMaatInfo(PaneelToewijzing toewijzing)
    {
        var kasten = ZoekKasten(toewijzing.KastIds);
        return kasten.Count == 0 ? null : BerekenPaneelMaatInfo(toewijzing, kasten);
    }

    private PaneelMaatInfo? BerekenPaneelMaatInfo(PaneelToewijzing toewijzing, List<Kast> kasten)
    {
        var dragendeKastIds = kasten.Select(kast => kast.Id).ToHashSet();
        var wand = Wanden.FirstOrDefault(item => item.KastIds.Any(dragendeKastIds.Contains));

        List<Kast> wandKasten = wand is null ? [.. kasten] : KastenVoorWand(wand.Id);
        List<Apparaat> wandApparaten = wand is null ? [] : ApparatenVoorWand(wand.Id);
        List<PaneelGeometrieBron> paneelBronnen = wand is null ? [] : PaneelBronnenVoorWand(wand);

        return PaneelGeometrieService.BerekenVoorToewijzing(
            toewijzing,
            kasten,
            wandKasten,
            wandApparaten,
            paneelBronnen,
            PaneelRandSpeling)?.MaatInfo;
    }

    private List<PaneelGeometrieBron> PaneelBronnenVoorWand(KeukenWand wand)
    {
        var wandKastIds = wand.KastIds.ToHashSet();
        return Toewijzingen
            .Where(item => item.KastIds.Any(wandKastIds.Contains))
            .Select(item => PaneelGeometrieService.MaakBronVoorToewijzing(item, ZoekKasten(item.KastIds)))
            .Where(bron => bron is not null)
            .Cast<PaneelGeometrieBron>()
            .ToList();
    }

    private bool VoegKastToeZonderNotify(Kast kast, Guid wandId, int? index = null)
    {
        var wand = Wanden.Find(item => item.Id == wandId);
        if (wand is null)
            return false;

        Kasten.Add(kast);
        if (index is int insertIndex)
            wand.KastIds.Insert(Math.Clamp(insertIndex, 0, wand.KastIds.Count), kast.Id);
        else
            wand.KastIds.Add(kast.Id);

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
        var wand = Wanden.Find(item => item.Id == wandId);
        if (wand is null)
            return false;

        Apparaten.Add(apparaat);
        if (index is int insertIndex)
            wand.ApparaatIds.Insert(Math.Clamp(insertIndex, 0, wand.ApparaatIds.Count), apparaat.Id);
        else
            wand.ApparaatIds.Add(apparaat.Id);

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
        => IndelingFormulierHelper.IsVrijeKastPlaatsing(
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
        var doelWand = Wanden.Find(item => item.Id == wandId);
        if (doelWand is null)
            return false;

        var huidigeWand = Wanden.Find(item => item.KastIds.Contains(kastId));
        if (huidigeWand?.Id == wandId)
        {
            if (index is not int insertIndex)
                return true;

            huidigeWand.KastIds.Remove(kastId);
            huidigeWand.KastIds.Insert(Math.Clamp(insertIndex, 0, huidigeWand.KastIds.Count), kastId);
            return true;
        }

        huidigeWand?.KastIds.Remove(kastId);
        doelWand.KastIds.Remove(kastId);

        if (index is int doelIndex)
            doelWand.KastIds.Insert(Math.Clamp(doelIndex, 0, doelWand.KastIds.Count), kastId);
        else
            doelWand.KastIds.Add(kastId);

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
