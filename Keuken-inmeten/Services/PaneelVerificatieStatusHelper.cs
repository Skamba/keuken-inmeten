namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public static class PaneelVerificatieStatusHelper
{
    public static PaneelVerificatieStatus LeesStatus(IReadOnlyList<PaneelVerificatieStatus> statussen, Guid toewijzingId)
        => statussen
            .Where(status => status.ToewijzingId == toewijzingId)
            .Select(KeukenDomeinValidatieService.NormaliseerVerificatieStatus)
            .FirstOrDefault()
        ?? new PaneelVerificatieStatus { ToewijzingId = toewijzingId };

    public static bool WerkStatusBij(
        List<PaneelVerificatieStatus> statussen,
        IReadOnlyList<PaneelToewijzing> toewijzingen,
        Guid toewijzingId,
        bool matenOk,
        bool scharnierPositiesOk)
    {
        if (toewijzingen.All(toewijzing => toewijzing.Id != toewijzingId))
            return false;

        var bestaandeStatus = statussen.FirstOrDefault(status => status.ToewijzingId == toewijzingId);
        if (bestaandeStatus is not null &&
            bestaandeStatus.MatenOk == matenOk &&
            bestaandeStatus.ScharnierPositiesOk == scharnierPositiesOk)
        {
            return false;
        }

        if (bestaandeStatus is null)
        {
            statussen.Add(new PaneelVerificatieStatus
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

        return true;
    }

    public static void VerwijderStatusVoorToewijzing(List<PaneelVerificatieStatus> statussen, Guid toewijzingId)
        => statussen.RemoveAll(status => status.ToewijzingId == toewijzingId);

    public static void VerwijderStatussenVoorToewijzingen(
        List<PaneelVerificatieStatus> statussen,
        IEnumerable<Guid> toewijzingIds)
    {
        var ids = toewijzingIds.ToHashSet();
        if (ids.Count == 0)
            return;

        statussen.RemoveAll(status => ids.Contains(status.ToewijzingId));
    }
}
