namespace Keuken_inmeten.Models;

public class KastTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Naam { get; set; } = "";
    public KastType Type { get; set; }
    public double Breedte { get; set; }
    public double Hoogte { get; set; }
    public double Diepte { get; set; }
    public double Wanddikte { get; set; }
    public double GaatjesAfstand { get; set; }
    public double EersteGaatVanBoven { get; set; }
    public DateTime LaatstGebruikt { get; set; }
}
