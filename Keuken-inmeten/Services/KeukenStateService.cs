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

    public List<Kast> KastenVoorWand(Guid wandId)
    {
        var wand = Wanden.Find(w => w.Id == wandId);
        if (wand is null) return [];
        return wand.KastIds
            .Select(id => Kasten.Find(k => k.Id == id))
            .Where(k => k is not null)
            .ToList()!;
    }

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
        Kasten.Add(kast);
        var wand = Wanden.Find(w => w.Id == wandId);
        wand?.KastIds.Add(kast.Id);
        BijwerkenKastTemplate(kast);
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
        Apparaten.Add(apparaat);
        var wand = Wanden.Find(w => w.Id == wandId);
        wand?.ApparaatIds.Add(apparaat.Id);
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
        if (toewijzing.Type == PaneelType.Deur)
            LaatstGebruiktePotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(toewijzing.PotHartVanRand);

        Toewijzingen.Add(toewijzing);
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

    public List<PaneelResultaat> BerekenResultaten()
    {
        var resultaten = new List<PaneelResultaat>();

        foreach (var toewijzing in Toewijzingen)
        {
            var kasten = toewijzing.KastIds
                .Select(id => Kasten.Find(k => k.Id == id))
                .Where(k => k is not null)
                .ToList()!;
            if (kasten.Count > 0)
            {
                var maatInfo = BerekenPaneelMaatInfo(toewijzing, kasten!);
                resultaten.Add(ScharnierBerekeningService.BerekenPaneel(toewijzing, kasten!, maatInfo));
            }
        }

        return resultaten;
    }

    public PaneelMaatInfo? BerekenPaneelMaatInfo(PaneelToewijzing toewijzing)
    {
        var kasten = toewijzing.KastIds
            .Select(id => Kasten.Find(k => k.Id == id))
            .Where(k => k is not null)
            .Cast<Kast>()
            .ToList();

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
                var panelKasten = item.KastIds
                    .Select(id => Kasten.Find(k => k.Id == id))
                    .Where(k => k is not null)
                    .Cast<Kast>()
                    .ToList();
                return PaneelLayoutService.BerekenRechthoek(item, panelKasten);
            })
            .Where(rechthoek => rechthoek is not null)
            .Cast<PaneelRechthoek>();

        return [.. buurKasten, .. buurApparaten, .. buurPanelen];
    }
}
