using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class PaneelConfiguratie
{
    private void ResetConceptPaneel()
    {
        toonKastOpdelenModal = false;

        if (BewerkteToewijzing is { } bewerkteToewijzing)
        {
            LaadToewijzingInFormulier(bewerkteToewijzing);
            return;
        }

        var selectieBereik = PaneelLayoutService.BerekenOmhullende(geselecteerdeKasten);
        if (selectieBereik is null)
        {
            conceptPaneel = null;
            formToewijzing.XPositie = null;
            formToewijzing.HoogteVanVloer = null;
            formToewijzing.Breedte = 0;
            formToewijzing.Hoogte = 0;
            return;
        }

        conceptPaneel = BepaalStartRechthoek(selectieBereik);
        UpdateFormVanConcept();
    }

    private void ResetFormToewijzing()
        => formToewijzing = IndelingFormulierHelper.NieuwePaneelToewijzing(State.LaatstGebruiktePotHartVanRand);

    private void LaadToewijzingInFormulier(PaneelToewijzing toewijzing)
    {
        var paneelBron = PaneelBron(toewijzing);
        if (paneelBron is null)
        {
            bewerkToewijzingId = null;
            ResetConceptPaneel();
            return;
        }

        ResetFormToewijzing();
        formToewijzing.Type = toewijzing.Type;
        formToewijzing.ScharnierZijde = toewijzing.ScharnierZijde;
        formToewijzing.PotHartVanRand = ScharnierBerekeningService.NormaliseerCupCenterVanRand(toewijzing.PotHartVanRand);
        conceptPaneel = paneelBron.OpeningsRechthoek.Kopie();
        UpdateFormVanConcept();
    }

    private void UpdateFormVanConcept()
    {
        if (conceptPaneel is null)
            return;

        formToewijzing.XPositie = Math.Round(conceptPaneel.XPositie, 1);
        formToewijzing.HoogteVanVloer = Math.Round(conceptPaneel.HoogteVanVloer, 1);
        formToewijzing.Breedte = Math.Round(conceptPaneel.Breedte, 1);
        formToewijzing.Hoogte = Math.Round(conceptPaneel.Hoogte, 1);
    }

    private void VerwerkConceptPaneel(PaneelConceptWijziging wijziging)
    {
        var selectieBereik = PaneelLayoutService.BerekenOmhullende(geselecteerdeKasten);
        if (selectieBereik is null)
            return;

        var voorstel = PaneelLayoutService.ClampBinnen(wijziging.Paneel, selectieBereik);
        conceptPaneel = wijziging.Bewerking.StartsWith("input-", StringComparison.Ordinal)
            ? voorstel
            : SnapPaneel(wijziging.Bewerking, voorstel, selectieBereik);
        UpdateFormVanConcept();
    }

    private PaneelRechthoek SnapPaneel(string bewerking, PaneelRechthoek voorstel, PaneelRechthoek selectieBereik)
        => PaneelConfiguratieHelper.SnapPaneel(
            bewerking,
            voorstel,
            selectieBereik,
            XTargets(selectieBereik),
            YTargets(selectieBereik));

    private IEnumerable<double> XTargets(PaneelRechthoek selectieBereik)
    {
        yield return selectieBereik.XPositie;
        yield return selectieBereik.Rechterkant;

        foreach (var kast in geselecteerdeKasten)
        {
            yield return kast.XPositie;
            yield return kast.XPositie + kast.Breedte;
        }

        foreach (var paneel in BestaandePaneelRechthoeken())
        {
            yield return paneel.XPositie;
            yield return paneel.Rechterkant;
        }
    }

    private IEnumerable<double> YTargets(PaneelRechthoek selectieBereik)
    {
        yield return selectieBereik.HoogteVanVloer;
        yield return selectieBereik.Bovenzijde;

        foreach (var kast in geselecteerdeKasten)
        {
            yield return kast.HoogteVanVloer;
            yield return kast.HoogteVanVloer + kast.Hoogte;
        }

        foreach (var paneel in BestaandePaneelRechthoeken())
        {
            yield return paneel.HoogteVanVloer;
            yield return paneel.Bovenzijde;
        }
    }

    private IEnumerable<PaneelRechthoek> BestaandePaneelRechthoeken()
    {
        foreach (var paneelBron in ActievePaneelWerkruimte?.PaneelBronnen ?? [])
        {
            yield return paneelBron.OpeningsRechthoek.Kopie();
        }
    }

    private List<PaneelRechthoek> VrijeSegmentenVoorSelectie()
    {
        var selectieBereik = PaneelLayoutService.BerekenOmhullende(geselecteerdeKasten);
        return selectieBereik is null
            ? []
            : PaneelConfiguratieHelper.BepaalVrijeSegmenten(selectieBereik, BestaandePaneelRechthoeken());
    }

    private PaneelRechthoek BepaalStartRechthoek(PaneelRechthoek selectieBereik)
        => PaneelConfiguratieHelper.BepaalStartRechthoek(selectieBereik, VrijeSegmentenVoorSelectie());

    private void GebruikVrijSegment(PaneelRechthoek segment)
    {
        conceptPaneel = segment.Kopie();
        UpdateFormVanConcept();
    }

    private PaneelMaatInfo? BerekenConceptMaatInfo()
    {
        if (conceptPaneel is null || ActievePaneelWerkruimte is not { } werkruimte)
            return null;

        return PaneelGeometrieService.BerekenVoorConceptPaneel(
            conceptPaneel,
            werkruimte.Kasten,
            werkruimte.Apparaten,
            werkruimte.PaneelBronnen,
            State.PaneelRandSpeling,
            bewerkToewijzingId)?.MaatInfo;
    }

    private void PaneelOpslaan()
    {
        if (conceptPaneel is null || geselecteerdeKastIds.Count == 0)
            return;

        if (HeeftConflicterendPaneel())
        {
            Feedback.ToonFout("Dit paneel overlapt nog een bestaand paneel. Verplaats of verklein het eerst.");
            return;
        }

        var dragendeKasten = PaneelLayoutService.BepaalOverlappendeKasten(geselecteerdeKasten, conceptPaneel);
        if (dragendeKasten.Count == 0)
            return;

        var toewijzing = MaakPaneelToewijzing(conceptPaneel, dragendeKasten, bewerkToewijzingId);

        if (IsBewerkModus)
            State.WerkToewijzingBij(toewijzing);
        else
            State.VoegToewijzingToe(toewijzing);

        RondPaneelInvoerAf();
    }

    private PaneelGeometrieBron? PaneelBron(PaneelToewijzing toewijzing)
        => PaneelGeometrieService.MaakBronVoorToewijzing(toewijzing, State.ZoekKasten(toewijzing.KastIds));

    private void BewerkPaneel(Guid toewijzingId)
    {
        var toewijzing = State.ZoekGeindexeerdeToewijzing(toewijzingId)?.Toewijzing;
        if (toewijzing is null)
            return;

        StartPaneelBewerking(toewijzing);
    }

    private void AnnuleerBewerken()
        => ResetPaneelInvoer();

    private void VerwijderPaneel(Guid toewijzingId)
    {
        var toewijzingInfo = State.ZoekGeindexeerdeToewijzing(toewijzingId);
        if (toewijzingInfo is null)
            return;

        if (bewerkToewijzingId == toewijzingId)
            VerlaatPaneelBewerkmodus();

        var snapshot = new PaneelVerwijderSnapshot(KopieerToewijzing(toewijzingInfo.Toewijzing), toewijzingInfo.Index);
        State.VerwijderToewijzing(toewijzingId);
        ResetConceptPaneel();
        Feedback.ToonInfo(
            $"Paneel {toewijzingInfo.Index + 1} verwijderd.",
            "Ongedaan maken",
            () => HerstelPaneelAsync(snapshot));
    }

    private Guid? VindWandId(Guid kastId)
        => State.WandVoorKast(kastId)?.Id;

    private Guid? VindWandIdVoorToewijzing(PaneelToewijzing toewijzing)
        => toewijzing.KastIds
            .Select(VindWandId)
            .FirstOrDefault(wandId => wandId is not null);

    private Task HerstelPaneelAsync(PaneelVerwijderSnapshot snapshot)
    {
        var index = Math.Clamp(snapshot.Index, 0, State.Toewijzingen.Count);
        State.HerstelToewijzing(KopieerToewijzing(snapshot.Toewijzing), index);
        Feedback.ToonSucces($"Paneel {index + 1} is teruggezet.");
        return Task.CompletedTask;
    }

    private void RondPaneelInvoerAf()
    {
        SluitPaneelWerklaag();
        reviewWeergaveActief = false;
        ResetPaneelInvoer();
    }

    private PaneelToewijzing MaakPaneelToewijzing(PaneelRechthoek paneel, IReadOnlyList<Kast> dragendeKasten, Guid? toewijzingId = null) => new()
    {
        Id = toewijzingId ?? Guid.NewGuid(),
        Type = formToewijzing.Type,
        ScharnierZijde = formToewijzing.ScharnierZijde,
        PotHartVanRand = PotHartInput,
        KastIds = dragendeKasten.Select(kast => kast.Id).ToList(),
        Breedte = Math.Round(paneel.Breedte, 1),
        Hoogte = Math.Round(paneel.Hoogte, 1),
        XPositie = Math.Round(paneel.XPositie, 1),
        HoogteVanVloer = Math.Round(paneel.HoogteVanVloer, 1)
    };

    private static List<double> MaakStandaardOpdeelHoogtes(double beschikbareHoogte, int aantal)
        => PaneelConfiguratieHelper.MaakStandaardOpdeelHoogtes(beschikbareHoogte, aantal);

    private static PaneelToewijzing KopieerToewijzing(PaneelToewijzing bron)
        => IndelingFormulierHelper.KopieerToewijzing(bron);

    private sealed record PaneelVerwijderSnapshot(PaneelToewijzing Toewijzing, int Index);

    private static string VrijSegmentLabel(int index, PaneelRechthoek segment)
        => PaneelConfiguratieHelper.VrijSegmentLabel(index, segment);

    private static string TypeNaam(PaneelType type) => VisualisatieHelper.PaneelTypeLabel(type);

    private static string TypeBadgeClass(PaneelType type)
        => PaneelConfiguratieHelper.TypeBadgeClass(type);

    private static string FormatMm(double waarde) => $"{waarde:0.#} mm";
}
