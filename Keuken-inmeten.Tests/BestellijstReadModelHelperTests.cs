using Keuken_inmeten.Models;
using Keuken_inmeten.Pages;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class BestellijstReadModelHelperTests
{
    [Fact]
    public void Exportflow_transities_behouden_keuze_en_resetten_alleen_stap_en_open_status()
    {
        var exportFlow = BestellijstReadModelHelper.MaakStandaardExportFlow();

        exportFlow = BestellijstReadModelHelper.OpenExportFlow(exportFlow);
        exportFlow = BestellijstReadModelHelper.KiesExportType(exportFlow, BestellijstExportType.Excel);
        exportFlow = BestellijstReadModelHelper.GaNaarPreview(exportFlow);
        exportFlow = BestellijstReadModelHelper.GaNaarBevestiging(exportFlow);
        exportFlow = BestellijstReadModelHelper.GaTerug(exportFlow);

        Assert.True(exportFlow.ToonExportFlow);
        Assert.Equal(BestellijstExportFlowStap.Preview, exportFlow.Stap);
        Assert.Equal(BestellijstExportType.Excel, exportFlow.ExportType);

        exportFlow = BestellijstReadModelHelper.SluitExportFlow(exportFlow);

        Assert.False(exportFlow.ToonExportFlow);
        Assert.Equal(BestellijstExportFlowStap.Kiezen, exportFlow.Stap);
        Assert.Equal(BestellijstExportType.Excel, exportFlow.ExportType);
    }

    [Fact]
    public void BouwPaginaModel_normaliseert_paneeltype_en_bouwt_exportpreview_van_huidige_exportkeuze()
    {
        var state = MaakStateMetEenDeur();
        var exportFlow = BestellijstReadModelHelper.MaakStandaardExportFlow();
        exportFlow = BestellijstReadModelHelper.KiesExportType(exportFlow, BestellijstExportType.Excel);
        exportFlow = BestellijstReadModelHelper.StelPaneelTypeIn(exportFlow, "   ");
        exportFlow = BestellijstReadModelHelper.StelDikteIn(exportFlow, "18.4");

        var pagina = BestellijstReadModelHelper.BouwPaginaModel(
            state,
            exportFlow,
            new DateTime(2026, 4, 13, 12, 0, 0));

        var item = Assert.Single(pagina.Items);
        Assert.Null(pagina.RouteGate);
        Assert.Equal("Onbekend paneeltype", pagina.ExportDocument.PaneelType);
        Assert.Equal("18.4", pagina.ExportDocument.DikteMm);
        Assert.Equal(BestellijstExportType.Excel, pagina.GekozenExport.Type);
        Assert.Equal(item.Aantal, pagina.TotaalAantal);
        Assert.Equal(item.Aantal * item.Boorgaten.Count, pagina.TotaalBoorgaten);
        Assert.Contains("Excel bevat X/Y-kolommen", pagina.ExportPreviewPunten[2]);
    }

    [Fact]
    public void BouwPaginaModel_geeft_routegate_zolang_bestellijst_nog_niet_beschikbaar_is()
    {
        var pagina = BestellijstReadModelHelper.BouwPaginaModel(
            new KeukenStateService(),
            BestellijstReadModelHelper.MaakStandaardExportFlow(),
            new DateTime(2026, 4, 13, 12, 0, 0));

        Assert.NotNull(pagina.RouteGate);
        Assert.Empty(pagina.Items);
        Assert.Empty(pagina.WandGroepen);
        Assert.Empty(pagina.PaneelTypeTellingen);
        Assert.Equal(0, pagina.ExportDocument.Orderregels);
    }

    private static KeukenStateService MaakStateMetEenDeur()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand
        {
            Id = Guid.NewGuid(),
            Naam = "Achterwand",
            Breedte = 2400,
            Hoogte = 2700,
            PlintHoogte = 100
        };
        var kast = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Hoge kast",
            Type = KastType.HogeKast,
            Breedte = 600,
            Hoogte = 2200,
            Diepte = 560,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19,
            XPositie = 0,
            HoogteVanVloer = 0
        };

        state.VoegWandToe(wand);
        state.StelPaneelRandSpelingIn(0);
        state.VoegKastToe(kast, wand.Id);
        state.VoegToewijzingToe(new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            KastIds = [kast.Id],
            Type = PaneelType.Deur,
            ScharnierZijde = ScharnierZijde.Rechts,
            Breedte = 600,
            Hoogte = 2200
        });

        return state;
    }
}
