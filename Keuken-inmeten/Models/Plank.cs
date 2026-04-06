namespace Keuken_inmeten.Models;

public class Plank
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Hoogte van het midden van de plank, gemeten vanaf de binnenzijde van de kastbodem (mm).
    /// </summary>
    public double HoogteVanBodem { get; set; }
}
