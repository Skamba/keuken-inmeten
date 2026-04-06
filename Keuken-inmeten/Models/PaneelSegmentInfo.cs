namespace Keuken_inmeten.Models;

public class PaneelSegmentInfo
{
    public Kast Kast { get; set; } = default!;
    public double TopVanPaneel { get; set; }
    public double KastOffsetVanBoven { get; set; }
    public double Hoogte { get; set; }
}
