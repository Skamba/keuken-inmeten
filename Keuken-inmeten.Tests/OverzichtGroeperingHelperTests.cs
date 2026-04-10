using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class OverzichtGroeperingHelperTests
{
    [Fact]
    public void GroepeerPaneelToewijzingen_groepeert_op_wand_en_telt_types()
    {
        var state = new KeukenStateService();
        var wandLinks = new KeukenWand { Id = Guid.NewGuid(), Naam = "Links", Breedte = 2400, Hoogte = 2700, PlintHoogte = 100 };
        var wandRechts = new KeukenWand { Id = Guid.NewGuid(), Naam = "Rechts", Breedte = 1800, Hoogte = 2700, PlintHoogte = 100 };

        state.VoegWandToe(wandLinks);
        state.VoegWandToe(wandRechts);

        var kastLinks = MaakKast("Kast links", 0);
        var kastMidden = MaakKast("Kast midden", 600);
        var kastRechts = MaakKast("Kast rechts", 0);

        state.VoegKastToe(kastLinks, wandLinks.Id);
        state.VoegKastToe(kastMidden, wandLinks.Id);
        state.VoegKastToe(kastRechts, wandRechts.Id);

        state.VoegToewijzingToe(new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [kastLinks.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Links,
            PotHartVanRand = 22.5,
            Breedte = 597,
            Hoogte = 717,
            XPositie = 0,
            HoogteVanVloer = 100
        });
        state.VoegToewijzingToe(new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [kastMidden.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Rechts,
            PotHartVanRand = 22.5,
            Breedte = 597,
            Hoogte = 717
        });
        state.VoegToewijzingToe(new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [kastRechts.Id],
            Type = PaneelType.LadeFront,
            Breedte = 597,
            Hoogte = 177
        });

        var groepen = OverzichtGroeperingHelper.GroepeerPaneelToewijzingen(state);

        Assert.Collection(
            groepen,
            links =>
            {
                Assert.Equal("Links", links.WandNaam);
                Assert.Equal(2, links.AantalPanelen);
                Assert.Collection(
                    links.TypeTellingen,
                    telling =>
                    {
                        Assert.Equal("Deur", telling.Label);
                        Assert.Equal(2, telling.Aantal);
                    });
                Assert.Equal("Kast links", links.Items[0].KastenLabel);
                Assert.Contains("Scharnier links", links.Items[0].ScharnierLabel);
                Assert.Contains("Links 0 mm", links.Items[0].PlaatsingLabel);
            },
            rechts =>
            {
                Assert.Equal("Rechts", rechts.WandNaam);
                Assert.Equal(1, rechts.AantalPanelen);
                Assert.Collection(
                    rechts.TypeTellingen,
                    telling =>
                    {
                        Assert.Equal("Ladefront", telling.Label);
                        Assert.Equal(1, telling.Aantal);
                    });
            });
    }

    [Fact]
    public void GroepeerBestellijstOpWand_telt_orderregels_panelen_en_types()
    {
        var items = new List<BestellijstItem>
        {
            new()
            {
                Naam = "Deur 1",
                PaneelRolLabel = "Deur",
                WandNaam = "Links",
                Aantal = 2,
                Hoogte = 720,
                Breedte = 597
            },
            new()
            {
                Naam = "Ladefront 1",
                PaneelRolLabel = "Ladefront",
                WandNaam = "Links",
                Aantal = 3,
                Hoogte = 177,
                Breedte = 597
            },
            new()
            {
                Naam = "Blindpaneel 1",
                PaneelRolLabel = "Blindpaneel",
                WandNaam = "Rechts",
                Aantal = 1,
                Hoogte = 720,
                Breedte = 120
            }
        };

        var groepen = OverzichtGroeperingHelper.GroepeerBestellijstOpWand(items);

        Assert.Collection(
            groepen,
            links =>
            {
                Assert.Equal("Links", links.WandNaam);
                Assert.Equal(2, links.Orderregels);
                Assert.Equal(5, links.PanelenTotaal);
                Assert.Collection(
                    links.PaneelTypeTellingen,
                    telling =>
                    {
                        Assert.Equal("Ladefront", telling.Label);
                        Assert.Equal(3, telling.Aantal);
                    },
                    telling =>
                    {
                        Assert.Equal("Deur", telling.Label);
                        Assert.Equal(2, telling.Aantal);
                    });
            },
            rechts =>
            {
                Assert.Equal("Rechts", rechts.WandNaam);
                Assert.Equal(1, rechts.Orderregels);
                Assert.Equal(1, rechts.PanelenTotaal);
            });
    }

    [Fact]
    public void GroepeerBestellijstOpWand_gebruikt_wandidentiteit_bij_gelijke_namen()
    {
        var items = new List<BestellijstItem>
        {
            new()
            {
                Naam = "Ladefront 1",
                PaneelRolLabel = "Ladefront",
                WandId = Guid.NewGuid(),
                WandNaam = "Muur",
                Aantal = 3,
                Hoogte = 177,
                Breedte = 597
            },
            new()
            {
                Naam = "Ladefront 2",
                PaneelRolLabel = "Ladefront",
                WandId = Guid.NewGuid(),
                WandNaam = "Muur",
                Aantal = 6,
                Hoogte = 177,
                Breedte = 597
            }
        };

        var groepen = OverzichtGroeperingHelper.GroepeerBestellijstOpWand(items);

        Assert.Equal(2, groepen.Count);
        Assert.All(groepen, groep =>
        {
            Assert.Equal("Muur", groep.WandNaam);
            Assert.Equal(1, groep.Orderregels);
        });
        Assert.Equal([3, 6], groepen.Select(groep => groep.PanelenTotaal).OrderBy(totaal => totaal).ToArray());
    }

    private static Kast MaakKast(string naam, double xPositie) => new()
    {
        Id = Guid.NewGuid(),
        Naam = naam,
        Type = KastType.Onderkast,
        Breedte = 600,
        Hoogte = 720,
        Diepte = 560,
        Wanddikte = 18,
        GaatjesAfstand = 32,
        EersteGaatVanBoven = 19,
        HoogteVanVloer = 100,
        XPositie = xPositie
    };
}
