using Microsoft.JSInterop;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Components;

public partial class WandOpstelling
{
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
    public async Task<bool> OnPlankDrop(string kastIdStr, string plankIdStr, double hoogteVanBodem)
    {
        if (LeesAlleen) return false;
        if (!Guid.TryParse(kastIdStr, out var kastId)) return false;
        if (!Guid.TryParse(plankIdStr, out var plankId)) return false;

        var kast = Kasten.FirstOrDefault(k => k.Id == kastId);
        if (kast is null) return false;
        var plank = kast.Planken.FirstOrDefault(p => p.Id == plankId);
        if (plank is null) return false;

        if (Math.Abs(plank.HoogteVanBodem - hoogteVanBodem) < 0.05)
        {
            _geselecteerdeePlankKastId = kastId;
            _geselecteerdeePlankId = plankId;
            StateHasChanged();
            return false;
        }

        if (!OnPlankActie.HasDelegate)
            return false;

        await OnPlankActie.InvokeAsync(ComponentInteractieHelper.MaakPlankVerplaatsing(kastId, plankId, hoogteVanBodem));
        _geselecteerdeePlankKastId = kastId;
        _geselecteerdeePlankId = plankId;

        StateHasChanged();
        return true;
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
                var nieuwePositie = WandOpstellingHelper.BepaalKastPositieNaToets(
                    kast,
                    Kasten.Where(item => item.Id != kast.Id),
                    key,
                    stap,
                    WandBreedte,
                    WandHoogte,
                    PlintHoogte);
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

    private sealed record PlankVerwijderSnapshot(Guid KastId, Plank Plank, int Index);
}
