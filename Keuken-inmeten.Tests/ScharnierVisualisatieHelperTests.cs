using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class ScharnierVisualisatieHelperTests
{
    [Fact]
    public void Gebruikte_gaten_krimpen_bij_hoge_panelen_zodat_hartlijn_zichtbaar_blijft()
    {
        var onderbouwing = new BoorgatOnderbouwing
        {
            GaatBovenY = 2123,
            GaatOnderY = 2155
        };

        var scale = 480.0 / 2230.0;
        var radius = ScharnierVisualisatieHelper.BerekenGebruiktGatRadius(scale, onderbouwing);
        var hartafstand = (onderbouwing.GaatOnderY - onderbouwing.GaatBovenY) * scale;

        Assert.True(radius < hartafstand / 2.0,
            $"Radius {radius:0.###} moet kleiner zijn dan de halve hartafstand {hartafstand / 2.0:0.###}.");
        Assert.True(radius < ScharnierVisualisatieHelper.StandaardGebruiktGatRadius);
    }

    [Fact]
    public void Gebruikte_gaten_blijven_op_standaardgrootte_bij_ruime_schaal()
    {
        var onderbouwing = new BoorgatOnderbouwing
        {
            GaatBovenY = 69,
            GaatOnderY = 101
        };

        var radius = ScharnierVisualisatieHelper.BerekenGebruiktGatRadius(1.0, onderbouwing);

        Assert.Equal(ScharnierVisualisatieHelper.StandaardGebruiktGatRadius, radius);
    }
}
