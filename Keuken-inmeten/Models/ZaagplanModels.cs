namespace Keuken_inmeten.Models;

/// <summary>
/// Een rechthoekig paneel dat op een plaat geplaatst moet worden.
/// </summary>
public class ZaagplanPaneel
{
    public string Naam { get; set; } = "";
    public double Hoogte { get; set; }
    public double Breedte { get; set; }
    public int Aantal { get; set; } = 1;
}

/// <summary>
/// De plaatsing van één paneel op een plaat, met positie in mm.
/// </summary>
public class ZaagplanPlaatsing
{
    public string Naam { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Breedte { get; set; }
    public double Hoogte { get; set; }
    public bool Gedraaid { get; set; }
}

/// <summary>
/// Eén plaat met de panelen die erop geplaatst zijn.
/// </summary>
public class ZaagplanPlaat
{
    public int Nummer { get; set; }
    public double Breedte { get; set; }
    public double Hoogte { get; set; }
    public List<ZaagplanPlaatsing> Plaatsingen { get; set; } = [];
}

/// <summary>
/// Het volledige zaagplan: alle platen met hun plaatsingen en eventueel niet-geplaatste panelen.
/// </summary>
public class ZaagplanResultaat
{
    public List<ZaagplanPlaat> Platen { get; set; } = [];
    public List<ZaagplanPaneel> NietGeplaatst { get; set; } = [];
    public double Zaagbreedte { get; set; }
}
