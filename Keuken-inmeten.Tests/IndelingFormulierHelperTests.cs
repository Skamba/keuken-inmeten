using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class IndelingFormulierHelperTests
{
    [Fact]
    public void MaakKastMetVorigeWaarden_gebruikt_recentste_template_en_leegt_naam()
    {
        var templates = new[]
        {
            new KastTemplate
            {
                Naam = "Oud model",
                Breedte = 500,
                Hoogte = 720,
                Diepte = 560,
                Wanddikte = 18,
                GaatjesAfstand = 32,
                EersteGaatVanBoven = 19,
                LaatstGebruikt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new KastTemplate
            {
                Naam = "Hoge kast",
                Breedte = 600,
                Hoogte = 2100,
                Diepte = 600,
                Wanddikte = 19,
                GaatjesAfstand = 28,
                EersteGaatVanBoven = 17,
                LaatstGebruikt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        var kast = IndelingFormulierHelper.MaakKastMetVorigeWaarden(templates);

        Assert.Equal(string.Empty, kast.Naam);
        Assert.Equal(600d, kast.Breedte);
        Assert.Equal(2100d, kast.Hoogte);
        Assert.Equal(600d, kast.Diepte);
        Assert.Equal(KastType.HogeKast, kast.Type);
    }

    [Fact]
    public void BerekenMontageplaatPosities_maakt_links_en_rechts_per_standaardpositie()
    {
        var kast = new Kast
        {
            Hoogte = 1400,
            Wanddikte = 18,
            EersteGaatVanBoven = 19,
            GaatjesAfstand = 32
        };
        var verwachteAfstanden = ScharnierBerekeningService.BerekenStandaardPosities(
            kast.Hoogte,
            kast.EersteGaatPositieVanafBoven,
            kast.GaatjesAfstand);

        var posities = IndelingFormulierHelper.BerekenMontageplaatPosities(kast);

        Assert.Equal(verwachteAfstanden.Count * 2, posities.Count);
        foreach (var afstand in verwachteAfstanden)
        {
            Assert.Contains(posities, positie => positie.AfstandVanBoven == afstand && positie.Zijde == ScharnierZijde.Links);
            Assert.Contains(posities, positie => positie.AfstandVanBoven == afstand && positie.Zijde == ScharnierZijde.Rechts);
        }
    }

    [Fact]
    public void KopieerKast_maakt_losstaande_kopie_en_kan_plank_ids_vernieuwen()
    {
        var plankId = Guid.NewGuid();
        var bron = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Onderkast",
            Type = KastType.Onderkast,
            Breedte = 600,
            Hoogte = 720,
            Diepte = 560,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19,
            MontagePlaatPosities =
            [
                new MontagePlaatPositie { AfstandVanBoven = 100, Zijde = ScharnierZijde.Links }
            ],
            Planken =
            [
                new Plank { Id = plankId, HoogteVanBodem = 200 }
            ]
        };

        var kopie = IndelingFormulierHelper.KopieerKast(bron, behoudPlankIds: false);
        bron.Planken[0].HoogteVanBodem = 300;
        bron.MontagePlaatPosities[0].AfstandVanBoven = 140;

        Assert.NotSame(bron, kopie);
        Assert.NotSame(bron.Planken, kopie.Planken);
        Assert.NotSame(bron.MontagePlaatPosities, kopie.MontagePlaatPosities);
        Assert.NotEqual(plankId, kopie.Planken[0].Id);
        Assert.Equal(200d, kopie.Planken[0].HoogteVanBodem);
        Assert.Equal(100d, kopie.MontagePlaatPosities[0].AfstandVanBoven);
    }
}
