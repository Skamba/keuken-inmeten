using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class KeukenDomeinValidatieServiceTests
{
    [Fact]
    public void NormaliseerData_corrigeert_ongeldige_grenswaarden()
    {
        var data = new KeukenData
        {
            Wanden =
            [
                new KeukenWand
                {
                    Naam = "  Achterwand  ",
                    Breedte = -10,
                    Hoogte = 0,
                    PlintHoogte = -5,
                    KastIds = [Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("11111111-1111-1111-1111-111111111111")]
                }
            ],
            Kasten =
            [
                new Kast
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Naam = "  Kast  ",
                    Type = (KastType)99,
                    Breedte = -20,
                    Hoogte = 0,
                    Diepte = -30,
                    Wanddikte = -1,
                    GaatjesAfstand = 0,
                    EersteGaatVanBoven = -2,
                    XPositie = -15,
                    HoogteVanVloer = -25,
                    Planken = [new Plank { HoogteVanBodem = 9999 }]
                }
            ],
            Apparaten =
            [
                new Apparaat
                {
                    Naam = "  Oven  ",
                    Type = (ApparaatType)99,
                    Breedte = -1,
                    Hoogte = 0,
                    Diepte = -1,
                    XPositie = -20,
                    HoogteVanVloer = -50
                }
            ],
            Toewijzingen =
            [
                new PaneelToewijzing
                {
                    KastIds = [Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("11111111-1111-1111-1111-111111111111")],
                    Type = (PaneelType)99,
                    ScharnierZijde = (ScharnierZijde)99,
                    PotHartVanRand = 5,
                    Breedte = -10,
                    Hoogte = 0,
                    XPositie = -1,
                    HoogteVanVloer = -2
                }
            ],
            KastTemplates =
            [
                new KastTemplate
                {
                    Naam = "  Template  ",
                    Type = (KastType)99,
                    Breedte = -20,
                    Hoogte = 0,
                    Diepte = -30,
                    Wanddikte = -1,
                    GaatjesAfstand = 0,
                    EersteGaatVanBoven = -2
                }
            ]
        };

        var genormaliseerd = KeukenDomeinValidatieService.NormaliseerData(data);
        var wand = Assert.Single(genormaliseerd.Wanden);
        var kast = Assert.Single(genormaliseerd.Kasten);
        var apparaat = Assert.Single(genormaliseerd.Apparaten);
        var toewijzing = Assert.Single(genormaliseerd.Toewijzingen);
        var template = Assert.Single(genormaliseerd.KastTemplates);

        Assert.Equal("Achterwand", wand.Naam);
        Assert.Equal(3000, wand.Breedte);
        Assert.Equal(2600, wand.Hoogte);
        Assert.Equal(0, wand.PlintHoogte);
        Assert.Single(wand.KastIds);

        Assert.Equal("Kast", kast.Naam);
        Assert.Equal(KastType.Onderkast, kast.Type);
        Assert.Equal(600, kast.Breedte);
        Assert.Equal(720, kast.Hoogte);
        Assert.Equal(560, kast.Diepte);
        Assert.Equal(18, kast.Wanddikte);
        Assert.Equal(32, kast.GaatjesAfstand);
        Assert.Equal(19, kast.EersteGaatVanBoven);
        Assert.Equal(0, kast.XPositie);
        Assert.Equal(0, kast.HoogteVanVloer);
        Assert.Single(kast.Planken);
        Assert.Equal(720, kast.Planken[0].HoogteVanBodem);
        Assert.NotEmpty(kast.MontagePlaatPosities);

        Assert.Equal("Oven", apparaat.Naam);
        Assert.Equal(ApparaatType.Oven, apparaat.Type);
        Assert.Equal(600, apparaat.Breedte);
        Assert.Equal(600, apparaat.Hoogte);
        Assert.Equal(560, apparaat.Diepte);
        Assert.Equal(0, apparaat.XPositie);
        Assert.Equal(0, apparaat.HoogteVanVloer);

        Assert.Equal(PaneelType.Deur, toewijzing.Type);
        Assert.Equal(ScharnierZijde.Links, toewijzing.ScharnierZijde);
        Assert.Equal(ScharnierBerekeningService.MinCupCenterVanRand, toewijzing.PotHartVanRand);
        Assert.Equal(1, toewijzing.Breedte);
        Assert.Equal(1, toewijzing.Hoogte);
        Assert.Equal(0, toewijzing.XPositie);
        Assert.Equal(0, toewijzing.HoogteVanVloer);
        Assert.Single(toewijzing.KastIds);

        Assert.Equal("Template", template.Naam);
        Assert.Equal(KastType.Onderkast, template.Type);
        Assert.Equal(600, template.Breedte);
        Assert.Equal(720, template.Hoogte);
    }
}
