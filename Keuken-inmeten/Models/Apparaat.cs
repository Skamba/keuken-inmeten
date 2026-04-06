namespace Keuken_inmeten.Models;

public enum ApparaatType
{
    Oven,
    Magnetron,
    Vaatwasser,
    Koelkast,
    Vriezer,
    Kookplaat,
    Afzuigkap
}

public class Apparaat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Naam { get; set; } = "";
    public ApparaatType Type { get; set; } = ApparaatType.Oven;
    public double Breedte { get; set; } = 600;
    public double Hoogte { get; set; } = 600;
    public double Diepte { get; set; } = 560;
    public double HoogteVanVloer { get; set; }
    public double XPositie { get; set; }

    public static (double breedte, double hoogte, double diepte) StandaardAfmetingen(ApparaatType type) => type switch
    {
        ApparaatType.Oven => (600, 600, 560),
        ApparaatType.Magnetron => (600, 380, 400),
        ApparaatType.Vaatwasser => (600, 820, 560),
        ApparaatType.Koelkast => (600, 1800, 600),
        ApparaatType.Vriezer => (600, 850, 600),
        ApparaatType.Kookplaat => (600, 50, 520),
        ApparaatType.Afzuigkap => (600, 200, 400),
        _ => (600, 600, 560)
    };
}
