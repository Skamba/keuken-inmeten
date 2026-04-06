namespace Keuken_inmeten.Models;

public class BestellijstItem
{
    public string BasisNaam { get; set; } = "";
    public string Naam { get; set; } = "";
    public int Aantal { get; set; }
    public string PaneelRolLabel { get; set; } = "";
    public string WandNaam { get; set; } = "";
    public string KastenLabel { get; set; } = "";
    public string ContextLabel { get; set; } = "";
    public string ScharnierLabel { get; set; } = "";
    public double Hoogte { get; set; }
    public double Breedte { get; set; }
    public List<Boorgat> Boorgaten { get; set; } = [];
    public PaneelResultaat Resultaat { get; set; } = new();
}
