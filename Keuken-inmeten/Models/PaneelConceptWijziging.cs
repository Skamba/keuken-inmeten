namespace Keuken_inmeten.Models;

public class PaneelConceptWijziging
{
    public string Bewerking { get; set; } = "move";
    public PaneelRechthoek Paneel { get; set; } = new();
}
