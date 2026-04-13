namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public partial class KeukenStateService
{
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
        VerificatieStatussen.RemoveAll(status => status.ToewijzingId == id);
        NotifyChanged();
    }

    public bool HerstelToewijzing(PaneelToewijzing toewijzing, int index)
    {
        VoegToewijzingToeZonderNotify(KeukenDomeinValidatieService.NormaliseerToewijzing(toewijzing), index);
        NotifyChanged();
        return true;
    }

    public GeindexeerdeToewijzing? ZoekGeindexeerdeToewijzing(Guid toewijzingId)
    {
        var index = Toewijzingen.FindIndex(item => item.Id == toewijzingId);
        return index < 0 ? null : new GeindexeerdeToewijzing(Toewijzingen[index], index);
    }

    public PaneelWerkruimteContext? LeesPaneelWerkruimte(Guid wandId, Guid? uitsluitenToewijzingId = null)
        => BouwPaneelQueryContext().MaakWerkruimte(wandId, uitsluitenToewijzingId);

    public IReadOnlyList<PaneelWerkruimteContext> LeesPaneelWerkruimtes(Guid? uitsluitenToewijzingId = null)
    {
        var context = BouwPaneelQueryContext();
        return Wanden
            .Select(wand => context.MaakWerkruimte(wand.Id, uitsluitenToewijzingId))
            .Where(werkruimte => werkruimte is not null)
            .Cast<PaneelWerkruimteContext>()
            .ToList();
    }

    public PaneelVerificatieStatus LeesVerificatieStatus(Guid toewijzingId)
        => VerificatieStatussen
            .Where(status => status.ToewijzingId == toewijzingId)
            .Select(KeukenDomeinValidatieService.NormaliseerVerificatieStatus)
            .FirstOrDefault()
        ?? new PaneelVerificatieStatus { ToewijzingId = toewijzingId };

    public bool WerkVerificatieStatusBij(Guid toewijzingId, bool matenOk, bool scharnierPositiesOk)
    {
        if (Toewijzingen.All(toewijzing => toewijzing.Id != toewijzingId))
            return false;

        var bestaandeStatus = VerificatieStatussen.FirstOrDefault(status => status.ToewijzingId == toewijzingId);
        if (bestaandeStatus is not null &&
            bestaandeStatus.MatenOk == matenOk &&
            bestaandeStatus.ScharnierPositiesOk == scharnierPositiesOk)
        {
            return false;
        }

        if (bestaandeStatus is null)
        {
            VerificatieStatussen.Add(new PaneelVerificatieStatus
            {
                ToewijzingId = toewijzingId,
                MatenOk = matenOk,
                ScharnierPositiesOk = scharnierPositiesOk
            });
        }
        else
        {
            bestaandeStatus.MatenOk = matenOk;
            bestaandeStatus.ScharnierPositiesOk = scharnierPositiesOk;
        }

        NotifyChanged();
        return true;
    }

    public List<PaneelResultaat> BerekenResultaten()
    {
        var context = BouwPaneelQueryContext();
        var resultaten = new List<PaneelResultaat>();

        foreach (var toewijzing in Toewijzingen)
        {
            var kasten = context.KastenVoorToewijzing(toewijzing);
            if (kasten.Count > 0)
            {
                var maatInfo = BerekenPaneelMaatInfo(toewijzing, kasten, context);
                resultaten.Add(ScharnierBerekeningService.BerekenPaneel(toewijzing, kasten, maatInfo));
            }
        }

        return resultaten;
    }

    public PaneelMaatInfo? BerekenPaneelMaatInfo(PaneelToewijzing toewijzing)
    {
        var context = BouwPaneelQueryContext();
        var kasten = context.KastenVoorToewijzing(toewijzing);
        return kasten.Count == 0 ? null : BerekenPaneelMaatInfo(toewijzing, kasten, context);
    }

    private PaneelMaatInfo? BerekenPaneelMaatInfo(
        PaneelToewijzing toewijzing,
        List<Kast> kasten,
        PaneelQueryContext context)
    {
        var wand = context.VindWandVoorToewijzing(toewijzing, kasten);

        List<Kast> wandKasten = wand is null ? [.. kasten] : context.KastenVoorWand(wand.Id);
        List<Apparaat> wandApparaten = wand is null ? [] : context.ApparatenVoorWand(wand.Id);
        List<PaneelGeometrieBron> paneelBronnen = wand is null ? [] : context.PaneelBronnenVoorWand(wand.Id);

        return PaneelGeometrieService.BerekenVoorToewijzing(
            toewijzing,
            kasten,
            wandKasten,
            wandApparaten,
            paneelBronnen,
            PaneelRandSpeling)?.MaatInfo;
    }

    private PaneelQueryContext BouwPaneelQueryContext()
        => new(
            Wanden,
            Toewijzingen,
            Kasten.ToDictionary(item => item.Id),
            Apparaten.ToDictionary(item => item.Id));

    private static List<T> ZoekEntiteitenOpLookup<T>(IEnumerable<Guid> ids, IReadOnlyDictionary<Guid, T> lookup)
        where T : class
        => ids
            .Select(lookup.GetValueOrDefault)
            .Where(item => item is not null)
            .Cast<T>()
            .ToList();

    private sealed class PaneelQueryContext
    {
        private readonly IReadOnlyList<KeukenWand> wanden;
        private readonly IReadOnlyList<PaneelToewijzing> toewijzingen;
        private readonly IReadOnlyDictionary<Guid, KeukenWand> wandLookup;
        private readonly IReadOnlyDictionary<Guid, Kast> kastenLookup;
        private readonly IReadOnlyDictionary<Guid, Apparaat> apparatenLookup;
        private readonly Dictionary<Guid, List<Kast>> kastenVoorToewijzingCache = [];
        private readonly Dictionary<Guid, KeukenWand?> wandVoorToewijzingCache = [];
        private readonly Dictionary<Guid, List<Kast>> kastenVoorWandCache = [];
        private readonly Dictionary<Guid, List<Apparaat>> apparatenVoorWandCache = [];
        private readonly Dictionary<Guid, List<PaneelToewijzing>> toewijzingenVoorWandCache = [];
        private readonly Dictionary<Guid, List<PaneelGeometrieBron>> paneelBronnenVoorWandCache = [];

        public PaneelQueryContext(
            IReadOnlyList<KeukenWand> wanden,
            IReadOnlyList<PaneelToewijzing> toewijzingen,
            IReadOnlyDictionary<Guid, Kast> kastenLookup,
            IReadOnlyDictionary<Guid, Apparaat> apparatenLookup)
        {
            this.wanden = wanden;
            this.toewijzingen = toewijzingen;
            wandLookup = wanden.ToDictionary(item => item.Id);
            this.kastenLookup = kastenLookup;
            this.apparatenLookup = apparatenLookup;
        }

        public PaneelWerkruimteContext? MaakWerkruimte(Guid wandId, Guid? uitsluitenToewijzingId = null)
        {
            var wand = wandLookup.GetValueOrDefault(wandId);
            return wand is null
                ? null
                : new PaneelWerkruimteContext(
                    wand,
                    KastenVoorWand(wandId),
                    ApparatenVoorWand(wandId),
                    ToewijzingenVoorWand(wandId, uitsluitenToewijzingId),
                    PaneelBronnenVoorWand(wandId, uitsluitenToewijzingId));
        }

        public List<Kast> KastenVoorToewijzing(PaneelToewijzing toewijzing)
        {
            if (!kastenVoorToewijzingCache.TryGetValue(toewijzing.Id, out var kasten))
            {
                kasten = ZoekEntiteitenOpLookup(toewijzing.KastIds, kastenLookup);
                kastenVoorToewijzingCache[toewijzing.Id] = kasten;
            }

            return kasten;
        }

        public KeukenWand? VindWandVoorToewijzing(PaneelToewijzing toewijzing, List<Kast> kasten)
        {
            if (!wandVoorToewijzingCache.TryGetValue(toewijzing.Id, out var wand))
            {
                var dragendeKastIds = kasten.Select(kast => kast.Id).ToHashSet();
                wand = wanden.FirstOrDefault(item => item.KastIds.Any(dragendeKastIds.Contains));
                wandVoorToewijzingCache[toewijzing.Id] = wand;
            }

            return wand;
        }

        public List<Kast> KastenVoorWand(Guid wandId)
        {
            if (!kastenVoorWandCache.TryGetValue(wandId, out var kasten))
            {
                var wand = wandLookup.GetValueOrDefault(wandId);
                kasten = wand is null ? [] : ZoekEntiteitenOpLookup(wand.KastIds, kastenLookup);
                kastenVoorWandCache[wandId] = kasten;
            }

            return kasten;
        }

        public List<Apparaat> ApparatenVoorWand(Guid wandId)
        {
            if (!apparatenVoorWandCache.TryGetValue(wandId, out var apparaten))
            {
                var wand = wandLookup.GetValueOrDefault(wandId);
                apparaten = wand is null ? [] : ZoekEntiteitenOpLookup(wand.ApparaatIds, apparatenLookup);
                apparatenVoorWandCache[wandId] = apparaten;
            }

            return apparaten;
        }

        public List<PaneelToewijzing> ToewijzingenVoorWand(Guid wandId, Guid? uitsluitenToewijzingId = null)
        {
            if (!toewijzingenVoorWandCache.TryGetValue(wandId, out var wandToewijzingen))
            {
                var wand = wandLookup.GetValueOrDefault(wandId);
                if (wand is null)
                {
                    wandToewijzingen = [];
                }
                else
                {
                    var wandKastIds = wand.KastIds.ToHashSet();
                    wandToewijzingen = toewijzingen
                        .Where(item => item.KastIds.Any(wandKastIds.Contains))
                        .ToList();
                }

                toewijzingenVoorWandCache[wandId] = wandToewijzingen;
            }

            return uitsluitenToewijzingId is Guid toewijzingId
                ? wandToewijzingen.Where(item => item.Id != toewijzingId).ToList()
                : wandToewijzingen;
        }

        public List<PaneelGeometrieBron> PaneelBronnenVoorWand(Guid wandId, Guid? uitsluitenToewijzingId = null)
        {
            if (!paneelBronnenVoorWandCache.TryGetValue(wandId, out var paneelBronnen))
            {
                paneelBronnen = ToewijzingenVoorWand(wandId)
                    .Select(item => PaneelGeometrieService.MaakBronVoorToewijzing(item, KastenVoorToewijzing(item)))
                    .Where(bron => bron is not null)
                    .Cast<PaneelGeometrieBron>()
                    .ToList();
                paneelBronnenVoorWandCache[wandId] = paneelBronnen;
            }

            return uitsluitenToewijzingId is Guid toewijzingId
                ? paneelBronnen.Where(item => item.PaneelId != toewijzingId).ToList()
                : paneelBronnen;
        }
    }
}

public sealed record PaneelWerkruimteContext(
    KeukenWand Wand,
    IReadOnlyList<Kast> Kasten,
    IReadOnlyList<Apparaat> Apparaten,
    IReadOnlyList<PaneelToewijzing> Toewijzingen,
    IReadOnlyList<PaneelGeometrieBron> PaneelBronnen);
