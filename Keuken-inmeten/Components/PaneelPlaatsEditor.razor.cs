using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Keuken_inmeten.Services.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Keuken_inmeten.Components;

public partial class PaneelPlaatsEditor
{
    private sealed record PaneelVisual(PaneelToewijzing Toewijzing, PaneelRechthoek OpeningsRechthoek, PaneelRechthoek WerkRechthoek, string Kleur, string Label, string SubLabel);
    private sealed record HandlePositie(string Naam, double X, double Y);

    [Parameter, EditorRequired] public IReadOnlyList<Kast> Kasten { get; set; } = [];
    [Parameter] public IReadOnlyList<Apparaat> Apparaten { get; set; } = [];
    [Parameter] public double WandHoogte { get; set; } = 2600;
    [Parameter] public double WandBreedte { get; set; } = 3000;
    [Parameter] public double PlintHoogte { get; set; }
    [Parameter] public double TotaleRandSpeling { get; set; } = KeukenDomeinDefaults.ProjectDefaults.PaneelRandSpeling;
    [Parameter] public string? SvgMaxHoogte { get; set; }
    [Parameter] public double MaxVisueleHoogtePx { get; set; } = 500;
    [Parameter] public bool BeperkTotContainerBreedte { get; set; }
    [Parameter] public IReadOnlySet<Guid>? GeselecteerdeKastIds { get; set; }
    [Parameter] public IReadOnlyList<PaneelToewijzing>? Toewijzingen { get; set; }
    [Parameter] public IReadOnlyList<PaneelRechthoek>? VrijeSegmenten { get; set; }
    [Parameter] public PaneelRechthoek? ConceptPaneel { get; set; }
    [Parameter] public PaneelType ConceptPaneelType { get; set; } = PaneelType.Deur;
    [Parameter] public ScharnierZijde ConceptPaneelScharnierZijde { get; set; } = ScharnierZijde.Links;
    [Parameter] public EventCallback<Guid> OnKastGeselecteerd { get; set; }
    [Parameter] public EventCallback<PaneelConceptWijziging> OnConceptPaneelGewijzigd { get; set; }

    private ElementReference svgRef;
    private DotNetObjectReference<PaneelPlaatsEditor>? dotNetRef;
    private PaneelPlaatsEditorJsInterop? jsInterop;

    private const double P = 50;

    private double VisueleHoogtePx => Math.Max(MaxVisueleHoogtePx, 1);
    private double Schaal => VisueleHoogtePx / Math.Max(WandHoogte, 1);
    private double MuurHoogtePx => WandHoogte * Schaal;
    private double MuurBreedtePx => WandBreedte * Schaal;
    private double VloerY => P + MuurHoogtePx;
    private double SvgWidth => MuurBreedtePx + P * 2 + 30;
    private double SvgHeight => VisueleHoogtePx + P + 30;

    private IEnumerable<int> HoogteMarkeringen()
    {
        var step = WandHoogte > 2000 ? 500 : 250;
        for (var hoogte = step; hoogte < WandHoogte; hoogte += step)
            yield return hoogte;
    }

    private IEnumerable<int> BreedteMarkeringen()
    {
        var step = WandBreedte > 2000 ? 500 : 250;
        for (var breedte = step; breedte < WandBreedte; breedte += step)
            yield return breedte;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        dotNetRef = DotNetObjectReference.Create(this);
        jsInterop = new PaneelPlaatsEditorJsInterop(JS);
        await jsInterop.InitAsync(svgRef, dotNetRef);
    }

    [JSInvokable]
    public async Task OnKastKlik(string kastIdStr)
    {
        if (Guid.TryParse(kastIdStr, out var kastId))
        {
            await OnKastGeselecteerd.InvokeAsync(kastId);
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task OnConceptPaneelUpdate(string bewerking, double svgX, double svgY, double svgWidth, double svgHeight)
    {
        if (!OnConceptPaneelGewijzigd.HasDelegate)
            return;

        await OnConceptPaneelGewijzigd.InvokeAsync(
            ComponentInteractieHelper.MaakPaneelConceptWijziging(bewerking, svgX, svgY, svgWidth, svgHeight, P, Schaal, VloerY));

        StateHasChanged();
    }

    private IEnumerable<PaneelVisual> PaneelVisuals()
    {
        if (Toewijzingen is null)
            yield break;

        var paneelBronnen = PaneelBronnen();
        var paneelBronOpToewijzingId = paneelBronnen
            .Where(bron => bron.PaneelId is not null)
            .ToDictionary(bron => bron.PaneelId!.Value);

        var index = 0;
        foreach (var toewijzing in Toewijzingen)
        {
            if (!paneelBronOpToewijzingId.TryGetValue(toewijzing.Id, out var paneelBron))
                continue;

            var geometrie = PaneelGeometrieService.Bereken(
                paneelBron,
                Kasten,
                Apparaten,
                paneelBronnen,
                TotaleRandSpeling);
            var openingsRechthoek = geometrie.OpeningsRechthoek;
            var werkRechthoek = geometrie.WerkRechthoek;
            index++;
            yield return new PaneelVisual(
                toewijzing,
                openingsRechthoek,
                werkRechthoek,
                PaneelKleur(toewijzing.Type),
                $"{TypeLabel(toewijzing.Type)} {index}",
                $"{werkRechthoek.Breedte:0.#} × {werkRechthoek.Hoogte:0.#} mm");
        }
    }

    private PaneelMaatInfo? BerekenConceptMaatInfo()
    {
        if (ConceptPaneel is null)
            return null;

        return PaneelGeometrieService.BerekenVoorConceptPaneel(
            ConceptPaneel,
            Kasten,
            Apparaten,
            PaneelBronnen(),
            TotaleRandSpeling)?.MaatInfo;
    }

    private PaneelGeometrieBron? PaneelBron(PaneelToewijzing toewijzing)
        => PaneelGeometrieService.MaakBronVoorToewijzing(
            toewijzing,
            Kasten.Where(kast => toewijzing.KastIds.Contains(kast.Id)).ToList());

    private List<PaneelGeometrieBron> PaneelBronnen()
    {
        if (Toewijzingen is null)
            return [];

        return Toewijzingen
            .Select(PaneelBron)
            .Where(bron => bron is not null)
            .Cast<PaneelGeometrieBron>()
            .ToList();
    }

    private static IEnumerable<HandlePositie> DraftHandles(double x, double y, double w, double h)
    {
        yield return new HandlePositie("nw", x, y);
        yield return new HandlePositie("ne", x + w, y);
        yield return new HandlePositie("se", x + w, y + h);
        yield return new HandlePositie("sw", x, y + h);
    }

    private static string PaneelKleur(PaneelType type) => type switch
    {
        PaneelType.Deur => "#0d6efd",
        PaneelType.LadeFront => "#e67e22",
        _ => "#6c757d"
    };

    private static string TypeLabel(PaneelType type) => type switch
    {
        PaneelType.Deur => "Deur",
        PaneelType.LadeFront => "Lade",
        PaneelType.BlindPaneel => "Blind",
        _ => type.ToString()
    };

    private static string KastKleur(KastType type) => VisualisatieHelper.KastKleur(type);

    private static string ApparaatKleur(ApparaatType type) => VisualisatieHelper.ApparaatKleur(type);

    private static string Fmt(double value) => VisualisatieHelper.Fmt(value);

    public async ValueTask DisposeAsync()
    {
        if (jsInterop is not null)
        {
            await jsInterop.DisposeSvgAsync(svgRef);
            await jsInterop.DisposeAsync();
        }

        dotNetRef?.Dispose();
    }
}
