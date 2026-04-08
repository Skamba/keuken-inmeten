using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class ComponentInteractieHelperTests
{
    [Fact]
    public void BepaalKastVolgordeWijziging_corrigeert_drop_na_sleep_naar_rechts()
    {
        var wijziging = ComponentInteractieHelper.BepaalKastVolgordeWijziging(1, 4);

        Assert.NotNull(wijziging);
        Assert.Equal(1, wijziging!.VanIndex);
        Assert.Equal(3, wijziging.NaarIndex);
    }

    [Fact]
    public void MaakPlankHerstel_behoudt_callback_contract()
    {
        var kastId = Guid.NewGuid();
        var plankId = Guid.NewGuid();

        var actie = ComponentInteractieHelper.MaakPlankHerstel(kastId, plankId, 384, 2);

        Assert.Equal(WandPlankActieType.Herstellen, actie.Type);
        Assert.Equal(kastId, actie.KastId);
        Assert.Equal(plankId, actie.PlankId);
        Assert.Equal(384, actie.HoogteVanBodem);
        Assert.Equal(2, actie.Index);
    }

    [Fact]
    public void MaakPaneelConceptWijziging_vertaalt_svg_coords_naar_mm_payload()
    {
        var wijziging = ComponentInteractieHelper.MaakPaneelConceptWijziging(
            bewerking: "resize",
            svgX: 150,
            svgY: 250,
            svgWidth: 200,
            svgHeight: 300,
            padding: 50,
            schaal: 0.2,
            vloerY: 550);

        Assert.Equal("resize", wijziging.Bewerking);
        Assert.Equal(500, wijziging.Paneel.XPositie);
        Assert.Equal(1000, wijziging.Paneel.Breedte);
        Assert.Equal(1500, wijziging.Paneel.Hoogte);
        Assert.Equal(0, wijziging.Paneel.HoogteVanVloer);
    }
}
