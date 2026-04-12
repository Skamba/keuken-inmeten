namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

public partial class KeukenStateService
{
    public List<KeukenWand> Wanden { get; } = [];
    public List<Kast> Kasten { get; } = [];
    public List<Apparaat> Apparaten { get; } = [];
    public List<PaneelToewijzing> Toewijzingen { get; } = [];
    public List<PaneelVerificatieStatus> VerificatieStatussen { get; } = [];
    public List<KastTemplate> KastTemplates { get; } = [];
    public double LaatstGebruiktePotHartVanRand { get; private set; } = KeukenDomeinDefaults.ProjectDefaults.LaatstGebruiktePotHartVanRand;
    public double PaneelRandSpeling { get; private set; } = KeukenDomeinDefaults.ProjectDefaults.PaneelRandSpeling;

    /// <summary>Fires after any state mutation so subscribers can persist the data.</summary>
    public event Action? OnStateChanged;

    private void NotifyChanged() => OnStateChanged?.Invoke();

    public KeukenData Exporteren() => new()
    {
        Wanden = [.. Wanden],
        Kasten = [.. Kasten],
        Apparaten = [.. Apparaten],
        Toewijzingen = [.. Toewijzingen],
        VerificatieStatussen = [.. VerificatieStatussen.Select(KeukenDomeinValidatieService.NormaliseerVerificatieStatus)],
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
        VerificatieStatussen.Clear();
        KastTemplates.Clear();

        Wanden.AddRange(genormaliseerd.Wanden);
        Kasten.AddRange(genormaliseerd.Kasten);
        Apparaten.AddRange(genormaliseerd.Apparaten);
        Toewijzingen.AddRange(genormaliseerd.Toewijzingen);
        VerificatieStatussen.AddRange(genormaliseerd.VerificatieStatussen);
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
           !ZijnBijnaGelijk(LaatstGebruiktePotHartVanRand, KeukenDomeinDefaults.ProjectDefaults.LaatstGebruiktePotHartVanRand) ||
           !ZijnBijnaGelijk(PaneelRandSpeling, KeukenDomeinDefaults.ProjectDefaults.PaneelRandSpeling);

    public void Importeer(KeukenData data)
    {
        Laden(data);
        NotifyChanged();
    }

    public void VerwijderAlles()
        => Importeer(new KeukenData());
}
