using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class BestellijstServiceTests
{
    [Fact]
    public void Identieke_hoge_deuren_worden_gegroepeerd_met_aantal_en_heuristische_naam()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand
        {
            Id = Guid.NewGuid(),
            Naam = "Muur",
            Breedte = 2400,
            Hoogte = 2700,
            PlintHoogte = 100
        };

        state.VoegWandToe(wand);

        var kastLinks = MaakHogeKast("Hoge kast links", xPositie: 0);
        var kastRechts = MaakHogeKast("Hoge kast rechts", xPositie: 600);

        state.VoegKastToe(kastLinks, wand.Id);
        state.VoegKastToe(kastRechts, wand.Id);

        state.VoegToewijzingToe(MaakDeurToewijzing(kastLinks.Id));
        state.VoegToewijzingToe(MaakDeurToewijzing(kastRechts.Id));

        var items = BestellijstService.BerekenItems(state);

        var item = Assert.Single(items);
        Assert.Equal("Hoge Deur 1", item.Naam);
        Assert.Equal(2, item.Aantal);
        Assert.Equal(BestellijstService.StandaardAbsBandLabel, item.AbsBandLabel);
        Assert.Equal("Deur", item.PaneelRolLabel);
        Assert.Equal(4, item.Boorgaten.Count);
    }

    [Fact]
    public void Excel_export_bevat_boorgatkolommen_en_metadata()
    {
        var item = MaakBestellijstItem();

        var xml = BestellijstExportService.BouwExcelXml(
            [item],
            "MDF gelakt",
            "19",
            new DateTime(2026, 4, 6, 14, 0, 0));

        Assert.Contains("Paneeltype", xml);
        Assert.Contains("ABS-band", xml);
        Assert.Contains(BestellijstService.StandaardAbsBandLabel, xml);
        Assert.Contains("CNC nulpunt", xml);
        Assert.Contains(BestellijstExportService.CncNulpuntLabel, xml);
        Assert.Contains("Boorgat 1 X (links, mm)", xml);
        Assert.Contains("Boorgat 1 Y (boven, mm)", xml);
        Assert.DoesNotContain("Boorgat X (mm)", xml);
        Assert.DoesNotContain("Boorgat Y (mm)", xml);
        Assert.Contains("577.5", xml);
        Assert.Contains("Hoge Deur 1", xml);
        Assert.Contains("MDF gelakt", xml);
    }

    [Fact]
    public void Pdf_html_bevat_inline_visuals()
    {
        var item = MaakBestellijstItem();

        var html = BestellijstExportService.BouwPdfHtml(
            [item],
            "MDF gelakt",
            "19",
            new DateTime(2026, 4, 6, 14, 0, 0));

        Assert.Contains("Bestellijst", html);
        Assert.Contains("ABS-band", html);
        Assert.Contains(BestellijstService.StandaardAbsBandLabel, html);
        Assert.Contains(BestellijstExportService.CncNulpuntLabel, html);
        Assert.DoesNotContain(">X: ", html);
        Assert.DoesNotContain(">Y: ", html);
        Assert.Contains("Boorgaten", html);
        Assert.Contains("B1: X", html);
        Assert.Contains("Y 83 mm", html);
        Assert.Contains("Hoge Deur 1", html);
        Assert.Contains("<svg", html);
        Assert.Contains("window.print()", html);
    }

    [Fact]
    public void Cnc_x_waarde_wordt_vanaf_linkerbovenhoek_berekend()
    {
        var item = MaakBestellijstItem();

        var x = BestellijstExportService.BerekenCncX(item, item.Boorgaten[0]);
        var y = BestellijstExportService.BerekenCncY(item.Boorgaten[0]);

        Assert.Equal(577.5, x);
        Assert.Equal(83, y);
    }

    private static Kast MaakHogeKast(string naam, double xPositie) => new()
    {
        Id = Guid.NewGuid(),
        Naam = naam,
        Type = KastType.HogeKast,
        Breedte = 600,
        Hoogte = 2200,
        Diepte = 560,
        Wanddikte = 18,
        GaatjesAfstand = 32,
        EersteGaatVanBoven = 19,
        HoogteVanVloer = 0,
        XPositie = xPositie
    };

    private static PaneelToewijzing MaakDeurToewijzing(Guid kastId) => new()
    {
        Id = Guid.NewGuid(),
        KastIds = [kastId],
        Type = PaneelType.Deur,
        ScharnierZijde = ScharnierZijde.Rechts,
        Breedte = 600,
        Hoogte = 2200
    };

    private static BestellijstItem MaakBestellijstItem() => new()
    {
        BasisNaam = "Hoge Deur",
        Naam = "Hoge Deur 1",
        Aantal = 2,
        AbsBandLabel = BestellijstService.StandaardAbsBandLabel,
        PaneelRolLabel = "Deur",
        WandNaam = "Muur",
        KastenLabel = "Hoge kast links",
        ContextLabel = "Muur • Hoge kast links (+1 meer)",
        ScharnierLabel = "Rechts",
        Hoogte = 2200,
        Breedte = 600,
        Resultaat = new PaneelResultaat
        {
            KastNaam = "Hoge kast links",
            Type = PaneelType.Deur,
            Breedte = 600,
            Hoogte = 2200,
            ScharnierZijde = ScharnierZijde.Rechts,
            Boorgaten =
            [
                new Boorgat { X = 22.5, Y = 83 },
                new Boorgat { X = 22.5, Y = 606 },
                new Boorgat { X = 22.5, Y = 1118 }
            ]
        },
        Boorgaten =
        [
            new Boorgat { X = 22.5, Y = 83 },
            new Boorgat { X = 22.5, Y = 606 },
            new Boorgat { X = 22.5, Y = 1118 }
        ]
    };
}
