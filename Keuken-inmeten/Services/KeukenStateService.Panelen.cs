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
}
