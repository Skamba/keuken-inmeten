using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class PaneelMaatUitlegHelperTests
{
    [Fact]
    public void BreedteFormule_geeft_uitleg_in_gewone_taal_met_aftrek_per_rand()
    {
        var maatInfo = new PaneelMaatInfo
        {
            OpeningsRechthoek = new PaneelRechthoek { Breedte = 800, Hoogte = 600 },
            PaneelRechthoek = new PaneelRechthoek { Breedte = 796, Hoogte = 596 },
            RandSpelingPerRaakrand = 2,
            RaaktLinks = true,
            RaaktRechts = true
        };

        Assert.Equal(
            "800 mm maat in de opening - 2 mm links - 2 mm rechts = 796 mm maat om te bestellen",
            PaneelMaatUitlegHelper.BreedteFormule(maatInfo));
        Assert.Equal(
            "Links en rechts: aftrek op links en rechts doordat die randen een andere kast-, apparaat- of paneelrand raken.",
            PaneelMaatUitlegHelper.BreedteAftrekUitleg(maatInfo));
    }

    [Fact]
    public void HoogteFormule_noemt_geen_aftrek_als_randen_vrij_zijn()
    {
        var maatInfo = new PaneelMaatInfo
        {
            OpeningsRechthoek = new PaneelRechthoek { Breedte = 800, Hoogte = 600 },
            PaneelRechthoek = new PaneelRechthoek { Breedte = 800, Hoogte = 600 },
            RandSpelingPerRaakrand = 2
        };

        Assert.Equal(
            "600 mm maat in de opening - geen aftrek = 600 mm maat om te bestellen",
            PaneelMaatUitlegHelper.HoogteFormule(maatInfo));
        Assert.Equal(
            "Onder en boven: geen aftrek, deze randen zijn vrij.",
            PaneelMaatUitlegHelper.HoogteAftrekUitleg(maatInfo));
    }
}
