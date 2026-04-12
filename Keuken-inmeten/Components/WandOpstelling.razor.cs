using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Keuken_inmeten.Services.Interop;
using System.Globalization;

namespace Keuken_inmeten.Components;

public partial class WandOpstelling
{
    [Parameter, EditorRequired] public IReadOnlyList<Kast> Kasten { get; set; } = [];
    [Parameter] public IReadOnlyList<Apparaat> Apparaten { get; set; } = [];
    [Parameter] public double WandHoogte { get; set; } = 2600;
    [Parameter] public double WandBreedte { get; set; } = 3000;
    [Parameter] public double PlintHoogte { get; set; }
    [Parameter] public string? SvgMaxHoogte { get; set; }
    [Parameter] public bool BeperkTotContainerBreedte { get; set; } = true;
    [Parameter] public Guid? GeselecteerdeKastId { get; set; }
    [Parameter] public IReadOnlySet<Guid>? GeselecteerdeKastIds { get; set; }
    [Parameter] public bool LeesAlleen { get; set; }
    [Parameter] public EventCallback<Guid> OnKastGeselecteerd { get; set; }
    [Parameter] public EventCallback<Guid> OnKastKopieren { get; set; }
    [Parameter] public EventCallback OnKastPlakken { get; set; }
    [Parameter] public EventCallback<KastPositieWijziging> OnKastVerplaatst { get; set; }
    [Parameter] public EventCallback<ApparaatPositieWijziging> OnApparaatVerplaatst { get; set; }
    [Parameter] public EventCallback<WandPlankActie> OnPlankActie { get; set; }
    [Parameter] public IReadOnlyList<PaneelToewijzing>? Toewijzingen { get; set; }

    private Guid? _geselecteerdeePlankId;
    private Guid? _geselecteerdeePlankKastId;
    private DotNetObjectReference<WandOpstelling>? dotNetRef;
    private WandOpstellingJsInterop? jsInterop;
    private string SvgElementId { get; } = $"wand-opstelling-{Guid.NewGuid():N}";

    private const double P = 50;
    private const double MaxVisueleHoogte = 500;

    private double Schaal => MaxVisueleHoogte / Math.Max(WandHoogte, 1);
    private double MuurHoogtePx => WandHoogte * Schaal;
    private double MuurBreedtePx => WandBreedte * Schaal;
    private double VloerY => P + MuurHoogtePx;
    private double SvgWidth => MuurBreedtePx + P * 2 + 30;
    private double SvgHeight => MuurHoogtePx + P + 30;

    private IReadOnlyList<int> HoogteMarkeringen()
        => WandOpstellingHelper.BepaalHoogteMarkeringen(WandHoogte);

    private IReadOnlyList<int> BreedteMarkeringen()
        => WandOpstellingHelper.BepaalBreedteMarkeringen(WandBreedte);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            dotNetRef = DotNetObjectReference.Create(this);
            jsInterop = new WandOpstellingJsInterop(JS);
            await jsInterop.InitAsync(SvgElementId, dotNetRef, LeesAlleen);
        }
    }

    private static string KastKleur(KastType type) => VisualisatieHelper.KastKleur(type);

    private static string ApparaatKleur(ApparaatType type) => VisualisatieHelper.ApparaatKleur(type);

    private static string Fmt(double v) => VisualisatieHelper.Fmt(v);

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);

    private static string BouwPlankSnapData(Kast kast)
        => string.Join("|",
            PlankGaatjesHelper.BepaalSnapPunten(kast)
                .Select(snap => $"{snap.HoogteVanBodem.ToString("0.###", CultureInfo.InvariantCulture)}:{snap.GatIndex}"));

    private static string FormatPlankLabel(Kast kast, double hoogteVanBodem)
    {
        var hoogteLabel = hoogteVanBodem.ToString("0.#", CultureInfo.InvariantCulture);
        var snap = PlankGaatjesHelper.ZoekDichtstbijzijndeSnap(kast, hoogteVanBodem);
        return snap is null
            ? $"{hoogteLabel} mm"
            : $"{hoogteLabel} mm | gat {snap.Value.GatIndex}";
    }

    public async ValueTask DisposeAsync()
    {
        if (jsInterop is not null)
        {
            await jsInterop.DisposeSvgAsync(SvgElementId);
            await jsInterop.DisposeAsync();
        }
        dotNetRef?.Dispose();
    }
}
