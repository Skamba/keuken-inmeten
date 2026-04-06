namespace Keuken_inmeten.Models;

public class Boorgat
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Diameter { get; set; } = 35;
    public BoorgatOnderbouwing? Onderbouwing { get; set; }
}

public class BoorgatOnderbouwing
{
    public string KastNaam { get; set; } = "";
    public int GaatBovenIndex { get; set; }
    public int GaatOnderIndex { get; set; }
    public double GaatBovenY { get; set; }
    public double GaatOnderY { get; set; }
    public double MontagePlaatMiddenInKast { get; set; }
    public double IdealeY { get; set; }
    public double AfstandTotIdealeVerdeling { get; set; }
    public double AfstandTotBoven { get; set; }
    public double AfstandTotOnder { get; set; }
    public double? AfstandTotDichtstbijzijndeNaad { get; set; }
    public double? AfstandTotDichtstbijzijndePlank { get; set; }
}

public class PaneelResultaat
{
    public List<Guid> KastIds { get; set; } = [];
    public string KastNaam { get; set; } = "";
    public PaneelType Type { get; set; }
    public double Breedte { get; set; }
    public double Hoogte { get; set; }
    public ScharnierZijde ScharnierZijde { get; set; }
    public List<Boorgat> Boorgaten { get; set; } = [];
}
