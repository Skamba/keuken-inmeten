namespace Keuken_inmeten.Models;

public class PaneelMaatInfo
{
    public PaneelRechthoek OpeningsRechthoek { get; set; } = new();
    public PaneelRechthoek PaneelRechthoek { get; set; } = new();
    public double RandSpelingPerRaakrand { get; set; }
    public bool RaaktLinks { get; set; }
    public bool RaaktRechts { get; set; }
    public bool RaaktOnder { get; set; }
    public bool RaaktBoven { get; set; }

    public double InkortingLinks => RaaktLinks ? RandSpelingPerRaakrand : 0;
    public double InkortingRechts => RaaktRechts ? RandSpelingPerRaakrand : 0;
    public double InkortingOnder => RaaktOnder ? RandSpelingPerRaakrand : 0;
    public double InkortingBoven => RaaktBoven ? RandSpelingPerRaakrand : 0;
    public double TotaleInkortingBreedte => InkortingLinks + InkortingRechts;
    public double TotaleInkortingHoogte => InkortingOnder + InkortingBoven;
}
