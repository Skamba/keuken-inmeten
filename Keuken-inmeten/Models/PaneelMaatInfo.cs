namespace Keuken_inmeten.Models;

public class PaneelMaatInfo
{
    public PaneelRechthoek OpeningsRechthoek { get; set; } = new();
    public PaneelRechthoek PaneelRechthoek { get; set; } = new();
    public double TotaleRandSpeling { get; set; }
    public bool RaaktLinks { get; set; }
    public bool RaaktRechts { get; set; }
    public bool RaaktOnder { get; set; }
    public bool RaaktBoven { get; set; }
    public double InkortingLinks { get; set; }
    public double InkortingRechts { get; set; }
    public double InkortingOnder { get; set; }
    public double InkortingBoven { get; set; }
    public double TotaleInkortingBreedte => InkortingLinks + InkortingRechts;
    public double TotaleInkortingHoogte => InkortingOnder + InkortingBoven;
}
