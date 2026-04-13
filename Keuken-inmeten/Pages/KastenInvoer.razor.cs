using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Keuken_inmeten.Models;
using Keuken_inmeten.Services;

namespace Keuken_inmeten.Pages;

public partial class KastenInvoer
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "wand")]
    public Guid? Wand { get; set; }

    private string nieuweWandNaam = "";
    private Guid? bewerkWandId;
    private string bewerkWandNaam = "";
    private Guid? bevestigVerwijderWandId;
    private Guid? bevestigVerwijderKastId;
    private Guid? _clipboardKastId;
    private bool bevestigWisAlles;
    private bool toonWandToevoegenModal;

    private Guid? actieveWandId;
    private Kast formKast = NieuweKast();
    private bool isBewerken;
    private Guid? bewerkKastId;
    private bool toonKastFormulier;
    private bool toonTechnischeInstellingen;
    private bool technischeControleBevestigd;
    private int kastFormStap = 1;

    private Apparaat formApparaat = NieuwApparaat();
    private bool isApparaatBewerken;
    private Guid? bewerkApparaatId;
    private bool toonApparaatFormulier;
    private Guid? bevestigVerwijderApparaatId;
    private int apparaatFormStap = 1;

    protected override void OnInitialized()
        => State.OnStateChanged += HandleStateChanged;

    protected override void OnParametersSet()
    {
        Guid? gewensteWandId = Wand is Guid wandId && State.Wanden.Exists(wand => wand.Id == wandId)
            ? wandId
            : null;
        if (actieveWandId != gewensteWandId)
            StelActieveWandContextIn(gewensteWandId);
    }

    public void Dispose()
        => State.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged()
    {
        if (actieveWandId.HasValue && !State.Wanden.Exists(wand => wand.Id == actieveWandId.Value))
        {
            NavigeerNaarWand(null, replaceHistoryEntry: true);
            StelActieveWandContextIn(null);
        }

        _ = InvokeAsync(StateHasChanged);
    }

    private static Kast NieuweKast() => IndelingFormulierHelper.NieuweKast();

    private static Apparaat NieuwApparaat(ApparaatType type = ApparaatType.Oven)
        => IndelingFormulierHelper.NieuwApparaat(type);
}
