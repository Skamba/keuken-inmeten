namespace Keuken_inmeten.Models;

public static class KeukenDomeinDefaults
{
    public static class WandDefaults
    {
        public const double Hoogte = 2600;
        public const double Breedte = 3000;
        public const double PlintHoogte = 100;
    }

    public static class KastDefaults
    {
        public const KastType Type = KastType.Onderkast;
        public const double Breedte = 600;
        public const double Hoogte = 720;
        public const double Diepte = 560;
        public const double Wanddikte = 18;
        public const double GaatjesAfstand = 32;
        public const double EersteGaatVanBoven = 19;
    }

    public static KeukenWand NieuweWand() => new();

    public static Kast NieuweKast() => new();
}
