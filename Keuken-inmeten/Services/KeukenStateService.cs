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
        Wanden.Clear();
        Kasten.Clear();
        Apparaten.Clear();
        Toewijzingen.Clear();
        KastTemplates.Clear();

        Wanden.AddRange(data.Wanden);
        Kasten.AddRange(data.Kasten);
        Apparaten.AddRange(data.Apparaten);
        Toewijzingen.AddRange(data.Toewijzingen);
        KastTemplates.AddRange(data.KastTemplates);
        LaatstGebruiktePotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(data.LaatstGebruiktePotHartVanRand);
        PaneelRandSpeling = PaneelSpelingService.NormaliseerRandSpeling(data.PaneelRandSpeling);
    }

    // --- Wanden ---

    public void VoegWandToe(KeukenWand wand)
    {
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

    public void WerkWandBij(KeukenWand wand)
    {
        var index = Wanden.FindIndex(w => w.Id == wand.Id);
        if (index >= 0)
            Wanden[index] = wand;
        NotifyChanged();
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

        var gewijzigd = false;
        if (!ZijnBijnaGelijk(wand.Breedte, breedte))
        {
            wand.Breedte = breedte;
            gewijzigd = true;
        }

        if (!ZijnBijnaGelijk(wand.Hoogte, hoogte))
        {
            wand.Hoogte = hoogte;
            gewijzigd = true;
        }

        if (!ZijnBijnaGelijk(wand.PlintHoogte, plintHoogte))
        {
            wand.PlintHoogte = plintHoogte;
            gewijzigd = true;
        }

        if (!gewijzigd)
            return false;

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

    public void VoegKastToe(Kast kast, Guid wandId)
    {
        VoegKastToeZonderNotify(kast, wandId);
        NotifyChanged();
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

    public void WerkKastBij(Kast kast)
    {
        var index = Kasten.FindIndex(k => k.Id == kast.Id);
        if (index >= 0)
            Kasten[index] = kast;
        BijwerkenKastTemplate(kast);
        NotifyChanged();
    }

    public bool VerplaatsKast(Guid id, double xPositie, double hoogteVanVloer)
    {
        var kast = Kasten.Find(item => item.Id == id);
        if (kast is null)
            return false;

        if (ZijnBijnaGelijk(kast.XPositie, xPositie) && ZijnBijnaGelijk(kast.HoogteVanVloer, hoogteVanVloer))
            return false;

        kast.XPositie = xPositie;
        kast.HoogteVanVloer = hoogteVanVloer;
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
            HoogteVanBodem = hoogteVanBodem
        };

        VoegPlankToeZonderNotify(kast, plank, index);
        NotifyChanged();
        return plank;
    }

    public bool VerplaatsPlank(Guid kastId, Guid plankId, double hoogteVanBodem)
    {
        var kast = Kasten.Find(item => item.Id == kastId);
        var plank = kast?.Planken.Find(item => item.Id == plankId);
        if (plank is null || ZijnBijnaGelijk(plank.HoogteVanBodem, hoogteVanBodem))
            return false;

        plank.HoogteVanBodem = hoogteVanBodem;
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
            HoogteVanBodem = plank.HoogteVanBodem
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

        VoegKastToeZonderNotify(kast, wandId, kastIndex);
        foreach (var toewijzing in toewijzingen.OrderBy(item => item.Index))
            VoegToewijzingToeZonderNotify(toewijzing.Toewijzing, toewijzing.Index);

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

    public void VoegApparaatToe(Apparaat apparaat, Guid wandId)
    {
        VoegApparaatToeZonderNotify(apparaat, wandId);
        NotifyChanged();
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

    public void WerkApparaatBij(Apparaat apparaat)
    {
        var index = Apparaten.FindIndex(a => a.Id == apparaat.Id);
        if (index >= 0)
            Apparaten[index] = apparaat;
        NotifyChanged();
    }

    public bool VerplaatsApparaat(Guid id, double xPositie, double hoogteVanVloer)
    {
        var apparaat = Apparaten.Find(item => item.Id == id);
        if (apparaat is null)
            return false;

        if (ZijnBijnaGelijk(apparaat.XPositie, xPositie) && ZijnBijnaGelijk(apparaat.HoogteVanVloer, hoogteVanVloer))
            return false;

        apparaat.XPositie = xPositie;
        apparaat.HoogteVanVloer = hoogteVanVloer;
        NotifyChanged();
        return true;
    }

    public bool HerstelApparaat(Apparaat apparaat, Guid wandId, int index)
    {
        var wand = Wanden.Find(item => item.Id == wandId);
        if (wand is null)
            return false;

        VoegApparaatToeZonderNotify(apparaat, wandId, index);
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
        VoegToewijzingToeZonderNotify(toewijzing);
        NotifyChanged();
    }

    public void WerkToewijzingBij(PaneelToewijzing toewijzing)
    {
        var index = Toewijzingen.FindIndex(t => t.Id == toewijzing.Id);
        if (index < 0)
            return;

        if (toewijzing.Type == PaneelType.Deur)
            LaatstGebruiktePotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(toewijzing.PotHartVanRand);

        Toewijzingen[index] = toewijzing;
        NotifyChanged();
    }

    public void VerwijderToewijzing(Guid id)
    {
        Toewijzingen.RemoveAll(t => t.Id == id);
        NotifyChanged();
    }

    public bool HerstelToewijzing(PaneelToewijzing toewijzing, int index)
    {
        VoegToewijzingToeZonderNotify(toewijzing, index);
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
        var openingsRechthoek = PaneelLayoutService.BerekenRechthoek(toewijzing, kasten);
        if (openingsRechthoek is null)
            return null;

        var buurRechthoeken = BouwBuurRechthoeken(toewijzing, kasten);
        return PaneelSpelingService.BerekenMaatInfo(openingsRechthoek, buurRechthoeken, PaneelRandSpeling);
    }

    private List<PaneelRechthoek> BouwBuurRechthoeken(PaneelToewijzing toewijzing, List<Kast> dragendeKasten)
    {
        var dragendeKastIds = dragendeKasten.Select(kast => kast.Id).ToHashSet();
        var wand = Wanden.FirstOrDefault(item => item.KastIds.Any(dragendeKastIds.Contains));
        if (wand is null)
            return [];

        var buurKasten = Kasten
            .Where(kast => wand.KastIds.Contains(kast.Id) && !dragendeKastIds.Contains(kast.Id))
            .Select(PaneelLayoutService.NaarRechthoek);
        var buurApparaten = Apparaten
            .Where(apparaat => wand.ApparaatIds.Contains(apparaat.Id))
            .Select(PaneelLayoutService.NaarRechthoek);
        var buurPanelen = Toewijzingen
            .Where(item => item.Id != toewijzing.Id && item.KastIds.Any(wand.KastIds.Contains))
            .Select(item =>
            {
                var panelKasten = ZoekKasten(item.KastIds);
                return PaneelLayoutService.BerekenRechthoek(item, panelKasten);
            })
            .Where(rechthoek => rechthoek is not null)
            .Cast<PaneelRechthoek>();

        return [.. buurKasten, .. buurApparaten, .. buurPanelen];
    }

    private void VoegKastToeZonderNotify(Kast kast, Guid wandId, int? index = null)
    {
        Kasten.Add(kast);
        var wand = Wanden.Find(item => item.Id == wandId);
        if (wand is not null)
        {
            if (index is int insertIndex)
                wand.KastIds.Insert(Math.Clamp(insertIndex, 0, wand.KastIds.Count), kast.Id);
            else
                wand.KastIds.Add(kast.Id);
        }

        BijwerkenKastTemplate(kast);
    }

    private static void VoegPlankToeZonderNotify(Kast kast, Plank plank, int? index = null)
    {
        if (index is int insertIndex)
            kast.Planken.Insert(Math.Clamp(insertIndex, 0, kast.Planken.Count), plank);
        else
            kast.Planken.Add(plank);
    }

    private void VoegApparaatToeZonderNotify(Apparaat apparaat, Guid wandId, int? index = null)
    {
        Apparaten.Add(apparaat);
        var wand = Wanden.Find(item => item.Id == wandId);
        if (wand is not null)
        {
            if (index is int insertIndex)
                wand.ApparaatIds.Insert(Math.Clamp(insertIndex, 0, wand.ApparaatIds.Count), apparaat.Id);
            else
                wand.ApparaatIds.Add(apparaat.Id);
        }
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
}

public sealed record GeindexeerdeToewijzing(PaneelToewijzing Toewijzing, int Index);
