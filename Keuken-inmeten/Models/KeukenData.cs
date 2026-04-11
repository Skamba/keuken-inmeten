namespace Keuken_inmeten.Models;

using Keuken_inmeten.Services;

/// <summary>
/// Snapshot of all state for serialization to/from persistent storage.
/// </summary>
public class KeukenData
{
    public List<KeukenWand> Wanden { get; set; } = [];
    public List<Kast> Kasten { get; set; } = [];
    public List<Apparaat> Apparaten { get; set; } = [];
    public List<PaneelToewijzing> Toewijzingen { get; set; } = [];
    public List<PaneelVerificatieStatus> VerificatieStatussen { get; set; } = [];
    public List<KastTemplate> KastTemplates { get; set; } = [];
    public double LaatstGebruiktePotHartVanRand { get; set; } = ScharnierBerekeningService.CupCenterVanRand;
    public double PaneelRandSpeling { get; set; } = PaneelSpelingService.DefaultRandSpeling;
}
