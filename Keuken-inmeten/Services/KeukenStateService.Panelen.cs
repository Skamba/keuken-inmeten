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
        PaneelVerificatieStatusHelper.VerwijderStatusVoorToewijzing(VerificatieStatussen, id);
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
        => PaneelVerificatieStatusHelper.LeesStatus(VerificatieStatussen, toewijzingId);

    public bool WerkVerificatieStatusBij(Guid toewijzingId, bool matenOk, bool scharnierPositiesOk)
    {
        if (!PaneelVerificatieStatusHelper.WerkStatusBij(
                VerificatieStatussen,
                Toewijzingen,
                toewijzingId,
                matenOk,
                scharnierPositiesOk))
            return false;

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
}
