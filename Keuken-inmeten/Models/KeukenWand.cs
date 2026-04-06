namespace Keuken_inmeten.Models;

public class KeukenWand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Naam { get; set; } = "";
    public double Hoogte { get; set; } = 2600;
    public double Breedte { get; set; } = 3000;
    public double PlintHoogte { get; set; } = 100;
    public List<Guid> KastIds { get; set; } = [];
    public List<Guid> ApparaatIds { get; set; } = [];

    public double TotaleBreedte(List<Kast> alleKasten) =>
        KastIds.Sum(id => alleKasten.Find(k => k.Id == id)?.Breedte ?? 0);
}
