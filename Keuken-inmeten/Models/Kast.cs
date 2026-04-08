namespace Keuken_inmeten.Models;

public class Kast
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Naam { get; set; } = "";
    public KastType Type { get; set; } = KeukenDomeinDefaults.KastDefaults.Type;
    public double Breedte { get; set; } = KeukenDomeinDefaults.KastDefaults.Breedte;
    public double Hoogte { get; set; } = KeukenDomeinDefaults.KastDefaults.Hoogte;
    public double Diepte { get; set; } = KeukenDomeinDefaults.KastDefaults.Diepte;
    public double Wanddikte { get; set; } = KeukenDomeinDefaults.KastDefaults.Wanddikte;
    public double GaatjesAfstand { get; set; } = KeukenDomeinDefaults.KastDefaults.GaatjesAfstand;
    // User input is measured from the underside of the top shelf to the first hole.
    public double EersteGaatVanBoven { get; set; } = KeukenDomeinDefaults.KastDefaults.EersteGaatVanBoven;
    public double EersteGaatPositieVanafBoven => Wanddikte + EersteGaatVanBoven;
    public double HoogteVanVloer { get; set; }
    public double XPositie { get; set; }
    public List<MontagePlaatPositie> MontagePlaatPosities { get; set; } = [];
    public List<Plank> Planken { get; set; } = [];
}
