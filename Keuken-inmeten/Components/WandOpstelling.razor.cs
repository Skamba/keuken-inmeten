using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Keuken_inmeten.Services.Interop;

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

    private ElementReference svgRef;
    private Guid? _geselecteerdeePlankId;
    private Guid? _geselecteerdeePlankKastId;
    private DotNetObjectReference<WandOpstelling>? dotNetRef;
    private WandOpstellingJsInterop? jsInterop;

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
            await jsInterop.InitAsync(svgRef, dotNetRef, LeesAlleen);
        }
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
    public async Task OnDrop(string kastIdStr, double svgX, double svgY)
    {
        if (LeesAlleen) return;

        if (kastIdStr.StartsWith("apparaat-"))
        {
            var apparaatIdStr = kastIdStr["apparaat-".Length..];
            if (!Guid.TryParse(apparaatIdStr, out var apparaatId)) return;

            var apparaat = Apparaten.FirstOrDefault(a => a.Id == apparaatId);
            if (apparaat is null) return;

            var apparaatPositie = WandOpstellingHelper.BepaalApparaatPositieNaDrop(
                apparaat,
                svgX,
                svgY,
                P,
                Schaal,
                VloerY,
                WandBreedte,
                WandHoogte);

            await OnApparaatVerplaatst.InvokeAsync(
                ComponentInteractieHelper.MaakApparaatVerplaatsing(apparaatId, apparaatPositie.XPositie, apparaatPositie.HoogteVanVloer));
            StateHasChanged();
            return;
        }

        if (!Guid.TryParse(kastIdStr, out var kastId)) return;

        var kast = Kasten.FirstOrDefault(k => k.Id == kastId);
        if (kast is null) return;

        var kastPositie = WandOpstellingHelper.BepaalKastPositieNaDrop(
            kast,
            Kasten.Where(k => k.Id != kastId),
            svgX,
            svgY,
            P,
            Schaal,
            VloerY,
            WandBreedte,
            WandHoogte,
            PlintHoogte);

        await OnKastVerplaatst.InvokeAsync(
            ComponentInteractieHelper.MaakKastVerplaatsing(kastId, kastPositie.XPositie, kastPositie.HoogteVanVloer));
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnPlankKlik(string kastIdStr, string plankIdStr)
    {
        if (LeesAlleen) return;
        if (Guid.TryParse(kastIdStr, out var kastId) && Guid.TryParse(plankIdStr, out var plankId))
        {
            _geselecteerdeePlankKastId = kastId;
            _geselecteerdeePlankId = plankId;
            await OnKastGeselecteerd.InvokeAsync(kastId);
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task OnPlankVerwijderen(string kastIdStr, string plankIdStr)
    {
        if (LeesAlleen) return;
        if (!Guid.TryParse(kastIdStr, out var kastId)) return;
        if (!Guid.TryParse(plankIdStr, out var plankId)) return;

        var kast = Kasten.FirstOrDefault(k => k.Id == kastId);
        var plank = kast?.Planken.FirstOrDefault(p => p.Id == plankId);
        if (kast is null || plank is null) return;

        await VerwijderPlankMetUndoAsync(kast, plank);
    }

    [JSInvokable]
    public async Task OnPlankDrop(string kastIdStr, string plankIdStr, double svgCenterY)
    {
        if (LeesAlleen) return;
        if (!Guid.TryParse(kastIdStr, out var kastId)) return;
        if (!Guid.TryParse(plankIdStr, out var plankId)) return;

        var kast = Kasten.FirstOrDefault(k => k.Id == kastId);
        if (kast is null) return;
        var plank = kast.Planken.FirstOrDefault(p => p.Id == plankId);
        if (plank is null) return;

        var hoogteVanBodem = WandOpstellingHelper.BepaalPlankHoogteNaDrop(kast, svgCenterY, VloerY, Schaal);

        if (OnPlankActie.HasDelegate)
            await OnPlankActie.InvokeAsync(ComponentInteractieHelper.MaakPlankVerplaatsing(kastId, plankId, hoogteVanBodem));
        _geselecteerdeePlankKastId = kastId;
        _geselecteerdeePlankId = plankId;

        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnPlankToevoegen(string kastIdStr, double svgY)
    {
        if (LeesAlleen) return;
        if (!Guid.TryParse(kastIdStr, out var kastId)) return;

        var kast = Kasten.FirstOrDefault(k => k.Id == kastId);
        if (kast is null) return;

        var hoogteVanBodem = WandOpstellingHelper.BepaalPlankHoogteVoorToevoegen(kast, svgY, VloerY, Schaal);

        var nieuwePlankId = Guid.NewGuid();
        if (OnPlankActie.HasDelegate)
            await OnPlankActie.InvokeAsync(ComponentInteractieHelper.MaakPlankToevoeging(kastId, nieuwePlankId, hoogteVanBodem));

        _geselecteerdeePlankKastId = kastId;
        _geselecteerdeePlankId = nieuwePlankId;

        await OnKastGeselecteerd.InvokeAsync(kastId);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnToets(string key, double stap)
    {
        if (LeesAlleen) return;
        if (_geselecteerdeePlankId.HasValue && _geselecteerdeePlankKastId.HasValue)
        {
            var kast = Kasten.FirstOrDefault(k => k.Id == _geselecteerdeePlankKastId.Value);
            var plank = kast?.Planken.FirstOrDefault(p => p.Id == _geselecteerdeePlankId.Value);
            if (plank != null && kast != null)
            {
                if (key == "Delete")
                {
                    await VerwijderPlankMetUndoAsync(kast, plank);
                    return;
                }

                var nieuweHoogte = WandOpstellingHelper.BepaalPlankHoogteNaToets(kast, plank, key, stap);
                if (nieuweHoogte.HasValue)
                    await OnPlankActie.InvokeAsync(ComponentInteractieHelper.MaakPlankVerplaatsing(kast.Id, plank.Id, nieuweHoogte.Value));
            }
        }
        else if (GeselecteerdeKastId.HasValue)
        {
            var kast = Kasten.FirstOrDefault(k => k.Id == GeselecteerdeKastId.Value);
            if (kast != null)
            {
                var nieuwePositie = WandOpstellingHelper.BepaalKastPositieNaToets(kast, key, stap, WandBreedte, WandHoogte);
                if (nieuwePositie is WandPositie positie)
                {
                    await OnKastVerplaatst.InvokeAsync(
                        ComponentInteractieHelper.MaakKastVerplaatsing(GeselecteerdeKastId.Value, positie.XPositie, positie.HoogteVanVloer));
                }
            }
        }
        StateHasChanged();
    }

    private async Task VerwijderPlankMetUndoAsync(Kast kast, Plank plank)
    {
        var index = kast.Planken.FindIndex(item => item.Id == plank.Id);
        var snapshot = new PlankVerwijderSnapshot(
            kast.Id,
            new Plank
            {
                Id = plank.Id,
                HoogteVanBodem = plank.HoogteVanBodem
            },
            index);

        if (OnPlankActie.HasDelegate)
            await OnPlankActie.InvokeAsync(ComponentInteractieHelper.MaakPlankVerwijdering(kast.Id, plank.Id, plank.HoogteVanBodem, index));

        if (_geselecteerdeePlankId == plank.Id)
        {
            _geselecteerdeePlankId = null;
            _geselecteerdeePlankKastId = null;
        }

        Feedback.ToonInfo(
            $"Plank in '{kast.Naam}' verwijderd.",
            "Ongedaan maken",
            () => HerstelPlankAsync(snapshot));
        StateHasChanged();
    }

    private async Task HerstelPlankAsync(PlankVerwijderSnapshot snapshot)
    {
        var kast = Kasten.FirstOrDefault(item => item.Id == snapshot.KastId);
        if (kast is null)
        {
            Feedback.ToonFout("Plank kan niet worden teruggezet omdat de kast niet meer bestaat.");
            return;
        }

        if (OnPlankActie.HasDelegate)
            await OnPlankActie.InvokeAsync(ComponentInteractieHelper.MaakPlankHerstel(snapshot.KastId, snapshot.Plank.Id, snapshot.Plank.HoogteVanBodem, snapshot.Index));

        _geselecteerdeePlankKastId = kast.Id;
        _geselecteerdeePlankId = snapshot.Plank.Id;

        Feedback.ToonSucces($"Plank in '{kast.Naam}' is teruggezet.");
        StateHasChanged();
    }

    private sealed record PlankVerwijderSnapshot(Guid KastId, Plank Plank, int Index);

    [JSInvokable]
    public async Task OnKopierenToets(string kastIdStr)
    {
        if (Guid.TryParse(kastIdStr, out var kastId))
            await OnKastKopieren.InvokeAsync(kastId);
    }

    [JSInvokable]
    public async Task OnPlakkenToets()
    {
        await OnKastPlakken.InvokeAsync();
    }

    private static string KastKleur(KastType type) => VisualisatieHelper.KastKleur(type);

    private static string ApparaatKleur(ApparaatType type) => VisualisatieHelper.ApparaatKleur(type);

    private static string Fmt(double v) => VisualisatieHelper.Fmt(v);

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
