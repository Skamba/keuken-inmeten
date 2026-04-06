namespace Keuken_inmeten.Models;

public class PaneelRechthoek
{
    public double XPositie { get; set; }
    public double HoogteVanVloer { get; set; }
    public double Breedte { get; set; }
    public double Hoogte { get; set; }

    public double Rechterkant => XPositie + Breedte;
    public double Bovenzijde => HoogteVanVloer + Hoogte;

    public PaneelRechthoek Kopie() => new()
    {
        XPositie = XPositie,
        HoogteVanVloer = HoogteVanVloer,
        Breedte = Breedte,
        Hoogte = Hoogte
    };
}
