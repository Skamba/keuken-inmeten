using Keuken_inmeten.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Keuken_inmeten.Layout;

public partial class NavMenu
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool collapseNavMenu = true;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    protected override void OnInitialized()
    {
        State.OnStateChanged += HandleStateChanged;
        Navigation.LocationChanged += HandleLocationChanged;
    }

    private void ToggleNavMenu()
        => collapseNavMenu = !collapseNavMenu;

    private void HandleStateChanged()
        => _ = InvokeAsync(StateHasChanged);

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
        => _ = InvokeAsync(StateHasChanged);

    private static string? BepaalWandNavTestIdPrefix(string stapId)
        => stapId switch
        {
            "kasten" => "indeling",
            "panelen" => "panelen",
            "verificatie" => "verificatie",
            _ => null
        };

    private static string MaakWandRoute(string stapId, Guid wandId)
        => stapId switch
        {
            "kasten" => $"kasten?wand={wandId:D}",
            "panelen" => $"panelen?wand={wandId:D}",
            "verificatie" => $"verificatie?wand={wandId:D}",
            _ => throw new ArgumentOutOfRangeException(nameof(stapId), stapId, "Onbekende stap voor wandnavigatie.")
        };

    private string? BepaalActieveWandNavigatieStapId()
    {
        var route = PersistentieDeelLinkHelper.BepaalRouteVoorHuidigeUrl(Navigation.ToBaseRelativePath(Navigation.Uri));

        return StappenFlowHelper.AlleStappen.FirstOrDefault(stap =>
            string.Equals(stap.Route, route, StringComparison.OrdinalIgnoreCase)
            && BepaalWandNavTestIdPrefix(stap.Id) is not null)?.Id;
    }

    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        Navigation.LocationChanged -= HandleLocationChanged;
    }
}
