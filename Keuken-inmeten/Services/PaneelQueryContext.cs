namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

internal sealed class PaneelQueryContext(
    IReadOnlyList<KeukenWand> wanden,
    IReadOnlyList<PaneelToewijzing> toewijzingen,
    IReadOnlyDictionary<Guid, Kast> kastenLookup,
    IReadOnlyDictionary<Guid, Apparaat> apparatenLookup)
{
    private readonly IReadOnlyDictionary<Guid, KeukenWand> wandLookup = wanden.ToDictionary(item => item.Id);
    private readonly Dictionary<Guid, List<Kast>> kastenVoorToewijzingCache = [];
    private readonly Dictionary<Guid, KeukenWand?> wandVoorToewijzingCache = [];
    private readonly Dictionary<Guid, List<Kast>> kastenVoorWandCache = [];
    private readonly Dictionary<Guid, List<Apparaat>> apparatenVoorWandCache = [];
    private readonly Dictionary<Guid, List<PaneelToewijzing>> toewijzingenVoorWandCache = [];
    private readonly Dictionary<Guid, List<PaneelGeometrieBron>> paneelBronnenVoorWandCache = [];

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

    private static List<T> ZoekEntiteitenOpLookup<T>(IEnumerable<Guid> ids, IReadOnlyDictionary<Guid, T> lookup)
        where T : class
        => ids
            .Select(lookup.GetValueOrDefault)
            .Where(item => item is not null)
            .Cast<T>()
            .ToList();
}

public sealed record PaneelWerkruimteContext(
    KeukenWand Wand,
    IReadOnlyList<Kast> Kasten,
    IReadOnlyList<Apparaat> Apparaten,
    IReadOnlyList<PaneelToewijzing> Toewijzingen,
    IReadOnlyList<PaneelGeometrieBron> PaneelBronnen);
