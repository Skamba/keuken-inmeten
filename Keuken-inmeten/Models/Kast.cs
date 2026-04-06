namespace Keuken_inmeten.Models;

public class Kast
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Naam { get; set; } = "";
    public KastType Type { get; set; } = KastType.Onderkast;
    public double Breedte { get; set; } = 600;
    public double Hoogte { get; set; } = 720;
    public double Diepte { get; set; } = 560;
    public double Wanddikte { get; set; } = 18;
    public double GaatjesAfstand { get; set; } = 32;
    // User input is measured from the underside of the top shelf to the first hole.
    public double EersteGaatVanBoven { get; set; } = 19;
    public double EersteGaatPositieVanafBoven => Wanddikte + EersteGaatVanBoven;
    public double HoogteVanVloer { get; set; }
    public double XPositie { get; set; }
    public List<MontagePlaatPositie> MontagePlaatPosities { get; set; } = [];
    public List<Plank> Planken { get; set; } = [];
}
