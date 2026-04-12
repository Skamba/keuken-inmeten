using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class KastenInvoer
{
    private KastVerwijderSnapshot? MaakKastSnapshot(Guid id)
    {
        var kast = State.Kasten.Find(item => item.Id == id);
        var wand = State.WandVoorKast(id);
        if (kast is null || wand is null)
            return null;

        var gekoppeldeToewijzingen = State.Toewijzingen
            .Select((toewijzing, index) => new ToewijzingSnapshot(KopieerToewijzing(toewijzing), index))
            .Where(snapshot => snapshot.Toewijzing.KastIds.Contains(id))
            .ToList();

        return new KastVerwijderSnapshot(
            KopieerKast(kast),
            wand.Id,
            wand.KastIds.IndexOf(id),
            gekoppeldeToewijzingen);
    }

    private Task HerstelKastAsync(KastVerwijderSnapshot snapshot)
    {
        var hersteld = State.HerstelKastMetToewijzingen(
            KopieerKast(snapshot.Kast),
            snapshot.WandId,
            snapshot.KastIndex,
            [.. snapshot.Toewijzingen.Select(item => new GeindexeerdeToewijzing(KopieerToewijzing(item.Toewijzing), item.Index))]);
        if (!hersteld)
        {
            Feedback.ToonFout("Kast kan niet worden teruggezet omdat de wand ontbreekt of de oude plek niet meer vrij is.");
            return Task.CompletedTask;
        }

        Feedback.ToonSucces($"Kast '{snapshot.Kast.Naam}' is teruggezet.");
        return Task.CompletedTask;
    }

    private static Kast KopieerKast(Kast bron, bool behoudPlankIds = true)
        => IndelingFormulierHelper.KopieerKast(bron, behoudPlankIds);

    private static PaneelToewijzing KopieerToewijzing(PaneelToewijzing bron)
        => IndelingFormulierHelper.KopieerToewijzing(bron);

    private sealed record ToewijzingSnapshot(PaneelToewijzing Toewijzing, int Index);

    private sealed record KastVerwijderSnapshot(
        Kast Kast,
        Guid WandId,
        int KastIndex,
        List<ToewijzingSnapshot> Toewijzingen);

    private sealed record ApparaatVerwijderSnapshot(Apparaat Apparaat, Guid WandId, int Index);
}
