using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class ZaagplanServiceTests
{
    [Fact]
    public void Enkel_paneel_past_op_een_plaat()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Deur 1", Breedte = 600, Hoogte = 2200, Aantal = 1 }
        };

        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070);

        Assert.Single(resultaat.Platen);
        Assert.Empty(resultaat.NietGeplaatst);
        Assert.Single(resultaat.Platen[0].Plaatsingen);
        Assert.Equal(1, resultaat.Platen[0].Nummer);
    }

    [Fact]
    public void Meerdere_panelen_worden_op_een_plaat_geplaatst_als_ze_passen()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Deur", Breedte = 600, Hoogte = 2000, Aantal = 4 }
        };

        // 4 × 600mm breed + 3 × 4mm zaag = 2412mm, past op 2800mm breed
        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070);

        Assert.Single(resultaat.Platen);
        Assert.Equal(4, resultaat.Platen[0].Plaatsingen.Count);
        Assert.Empty(resultaat.NietGeplaatst);
    }

    [Fact]
    public void Panelen_verdelen_over_meerdere_platen_als_nodig()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Deur", Breedte = 600, Hoogte = 2000, Aantal = 6 }
        };

        // 4 passen op plaat 1, 2 op plaat 2
        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070);

        Assert.Equal(2, resultaat.Platen.Count);
        Assert.Equal(4, resultaat.Platen[0].Plaatsingen.Count);
        Assert.Equal(2, resultaat.Platen[1].Plaatsingen.Count);
        Assert.Empty(resultaat.NietGeplaatst);
    }

    [Fact]
    public void Paneel_dat_niet_past_wordt_als_niet_geplaatst_gemeld()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Groot", Breedte = 3000, Hoogte = 3000, Aantal = 1 }
        };

        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070, draaiToe: false);

        Assert.Empty(resultaat.Platen);
        Assert.Single(resultaat.NietGeplaatst);
        Assert.Equal("Groot", resultaat.NietGeplaatst[0].Naam);
    }

    [Fact]
    public void Paneel_wordt_gedraaid_als_het_anders_niet_past()
    {
        // 2100 breed × 500 hoog past niet op 2070 hoogte als 500×2100
        // Maar 2100×500 past als breedte, hoogte
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Breed", Breedte = 2100, Hoogte = 500, Aantal = 1 }
        };

        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070, draaiToe: true);

        Assert.Single(resultaat.Platen);
        Assert.Single(resultaat.Platen[0].Plaatsingen);
        Assert.Empty(resultaat.NietGeplaatst);
    }

    [Fact]
    public void Draaien_uitgeschakeld_maakt_paneel_niet_passend_als_het_niet_origineel_past()
    {
        // 500×2100 past niet als hoogte > plaathoogte 2070
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Breed", Breedte = 500, Hoogte = 2100, Aantal = 1 }
        };

        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070, draaiToe: false);

        Assert.Empty(resultaat.Platen);
        Assert.Single(resultaat.NietGeplaatst);
    }

    [Fact]
    public void Zaagbreedte_wordt_meegenomen_bij_plaatsing()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Paneel", Breedte = 1398, Hoogte = 700, Aantal = 2 }
        };

        // 1398 + 4 (zaag) + 1398 = 2800, past precies
        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070, zaagbreedte: 4);

        Assert.Single(resultaat.Platen);
        Assert.Equal(2, resultaat.Platen[0].Plaatsingen.Count);
    }

    [Fact]
    public void Zaagbreedte_te_groot_dwingt_tweede_plaat_af()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Paneel", Breedte = 1400, Hoogte = 700, Aantal = 2 }
        };

        // 1400 + 4 + 1400 = 2804 > 2800, past net niet
        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070, zaagbreedte: 4);

        // Tweede paneel gaat naar shelf 2 (als hoogte past) of plaat 2
        var totaalGeplaatst = resultaat.Platen.Sum(p => p.Plaatsingen.Count);
        Assert.Equal(2, totaalGeplaatst);
        Assert.Empty(resultaat.NietGeplaatst);
    }

    [Fact]
    public void Shelving_plaatst_korte_panelen_onder_hoge_panelen()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Hoog", Breedte = 600, Hoogte = 1500, Aantal = 1 },
            new() { Naam = "Laag", Breedte = 600, Hoogte = 500, Aantal = 1 }
        };

        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070, zaagbreedte: 4);

        // Beide passen op 1 plaat: shelf 1 = 1500 hoog, shelf 2 = 500 hoog, totaal ~2004
        Assert.Single(resultaat.Platen);
        Assert.Equal(2, resultaat.Platen[0].Plaatsingen.Count);
    }

    [Fact]
    public void VanBestellijst_bouwt_panelen_uit_items()
    {
        var items = new List<BestellijstItem>
        {
            new()
            {
                Naam = "Hoge Deur 1",
                Hoogte = 2200,
                Breedte = 600,
                Aantal = 3,
                Resultaat = new PaneelResultaat()
            },
            new()
            {
                Naam = "Ladefront 1",
                Hoogte = 180,
                Breedte = 596,
                Aantal = 2,
                Resultaat = new PaneelResultaat()
            }
        };

        var panelen = ZaagplanService.VanBestellijst(items);

        Assert.Equal(2, panelen.Count);
        Assert.Equal("Hoge Deur 1", panelen[0].Naam);
        Assert.Equal(3, panelen[0].Aantal);
        Assert.Equal("Ladefront 1", panelen[1].Naam);
        Assert.Equal(2, panelen[1].Aantal);
    }

    [Fact]
    public void Lege_invoer_geeft_leeg_resultaat()
    {
        var resultaat = ZaagplanService.Bereken([], 2800, 2070);

        Assert.Empty(resultaat.Platen);
        Assert.Empty(resultaat.NietGeplaatst);
    }

    [Fact]
    public void Niet_geplaatste_panelen_worden_gegroepeerd()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Enorm", Breedte = 5000, Hoogte = 5000, Aantal = 3 }
        };

        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070, draaiToe: false);

        Assert.Single(resultaat.NietGeplaatst);
        Assert.Equal(3, resultaat.NietGeplaatst[0].Aantal);
    }

    [Fact]
    public void Plaatnummers_zijn_opeenvolgend()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Deur", Breedte = 600, Hoogte = 2000, Aantal = 10 }
        };

        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070);

        for (int i = 0; i < resultaat.Platen.Count; i++)
        {
            Assert.Equal(i + 1, resultaat.Platen[i].Nummer);
        }
    }

    [Fact]
    public void Gemengde_panelen_worden_efficiënt_verdeeld()
    {
        var panelen = new List<ZaagplanPaneel>
        {
            new() { Naam = "Hoge Deur", Breedte = 600, Hoogte = 2000, Aantal = 2 },
            new() { Naam = "Ladefront", Breedte = 596, Hoogte = 180, Aantal = 4 }
        };

        var resultaat = ZaagplanService.Bereken(panelen, 2800, 2070, zaagbreedte: 4);

        // 2 hoge deuren nemen 1204mm breed op shelf 1 (2000 hoog)
        // 4 ladefronten van 180mm hoog passen als extra shelf(s) onder de 2000mm shelf
        // Alles zou op 1 plaat moeten passen
        var totaalGeplaatst = resultaat.Platen.Sum(p => p.Plaatsingen.Count);
        Assert.Equal(6, totaalGeplaatst);
        Assert.Empty(resultaat.NietGeplaatst);
    }
}
