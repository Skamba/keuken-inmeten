namespace Keuken_inmeten.Models;

public class PaneelToewijzing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<Guid> KastIds { get; set; } = [];
    public PaneelType Type { get; set; } = PaneelType.Deur;
    public ScharnierZijde ScharnierZijde { get; set; } = ScharnierZijde.Links;
    public double Breedte { get; set; }
    public double Hoogte { get; set; }
    public double? XPositie { get; set; }
    public double? HoogteVanVloer { get; set; }
}
