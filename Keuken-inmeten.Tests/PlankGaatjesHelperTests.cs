using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PlankGaatjesHelperTests
{
    [Fact]
    public void ZoekDichtstbijzijndeSnap_geeft_hoogte_en_gatnummer_terug()
    {
        var kast = new Kast
        {
            Hoogte = 720,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19
        };

        var snap = PlankGaatjesHelper.ZoekDichtstbijzijndeSnap(kast, 347.2);

        Assert.NotNull(snap);
        Assert.Equal(345.0, snap.Value.HoogteVanBodem);
        Assert.Equal(11, snap.Value.GatIndex);
        Assert.Equal(357.0, snap.Value.GatVanBoven);
    }

    [Fact]
    public void BepaalSnapPunten_bevat_consistente_gatnummers_en_hoogtes()
    {
        var kast = new Kast
        {
            Hoogte = 720,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19
        };

        var snaps = PlankGaatjesHelper.BepaalSnapPunten(kast);

        Assert.Equal(21, snaps[0].GatIndex);
        Assert.Equal(677.0, snaps[0].GatVanBoven);
        Assert.Equal(25.0, snaps[0].HoogteVanBodem);
        Assert.Equal(1, snaps[^1].GatIndex);
        Assert.Equal(37.0, snaps[^1].GatVanBoven);
        Assert.Equal(665.0, snaps[^1].HoogteVanBodem);
        Assert.Equal(11, snaps.First(snap => snap.HoogteVanBodem == 345.0).GatIndex);
    }
}
