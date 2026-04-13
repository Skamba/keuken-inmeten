namespace Keuken_inmeten.Pages;

using Keuken_inmeten.Components;
using Keuken_inmeten.Models;
using System.Globalization;

public partial class KastenInvoer
{
    private KastenInvoerPaginaModel HuidigePaginaModel
        => KastenInvoerReadModelHelper.BouwPaginaModel(
            State.Wanden,
            State.Kasten,
            State.Apparaten,
            actieveWandId,
            toonKastFormulier,
            toonApparaatFormulier);
}

public static class KastenInvoerReadModelHelper
{
    public static KastenInvoerPaginaModel BouwPaginaModel(
        IReadOnlyList<KeukenWand> wanden,
        IReadOnlyList<Kast> kasten,
        IReadOnlyList<Apparaat> apparaten,
        Guid? actieveWandId,
        bool toonKastFormulier,
        bool toonApparaatFormulier)
    {
        var kastenLookup = kasten.ToDictionary(item => item.Id);
        var apparatenLookup = apparaten.ToDictionary(item => item.Id);

        var samenvattingen = wanden
            .Select(wand => BouwWandSamenvatting(wand, kastenLookup, apparatenLookup))
            .ToList();

        var actieveSamenvatting = actieveWandId is Guid wandId
            ? samenvattingen.FirstOrDefault(item => item.Wand.Id == wandId)
            : null;

        var actieveWerkruimte = actieveSamenvatting is null
            ? null
            : BouwActieveWerkruimte(actieveSamenvatting, toonKastFormulier, toonApparaatFormulier);

        return new(actieveWerkruimte);
    }

    private static KastenInvoerWandSamenvatting BouwWandSamenvatting(
        KeukenWand wand,
        IReadOnlyDictionary<Guid, Kast> kastenLookup,
        IReadOnlyDictionary<Guid, Apparaat> apparatenLookup)
    {
        var wandKasten = ZoekOpVolgorde(wand.KastIds, kastenLookup);
        var wandApparaten = ZoekOpVolgorde(wand.ApparaatIds, apparatenLookup);
        var gevuldeBreedte = wandKasten.Sum(item => item.Breedte);
        var vrijeBreedte = Math.Max(0, wand.Breedte - gevuldeBreedte);

        return new(
            Wand: wand,
            Kasten: wandKasten,
            Apparaten: wandApparaten,
            GevuldeBreedte: gevuldeBreedte,
            VrijeBreedte: vrijeBreedte);
    }

    private static KastenInvoerActieveWerkruimteModel BouwActieveWerkruimte(
        KastenInvoerWandSamenvatting samenvatting,
        bool toonKastFormulier,
        bool toonApparaatFormulier)
    {
        var werkstap = samenvatting.Kasten.Count == 0
            ? new KastenInvoerWerkstapModel(
                Kicker: "Nu doen",
                Titel: "Voeg eerst de eerste kast toe.",
                Beschrijving: "Gebruik een apparaat pas zodra het echt wandruimte of de vrije opening verandert.")
            : new KastenInvoerWerkstapModel(
                Kicker: "Volgende stap",
                Titel: null,
                Beschrijving: "Werk alleen bij wat op deze wand nog ontbreekt. Apparaten blijven optioneel zolang ze geen ruimte of vrije opening veranderen.");

        return new(
            Samenvatting: samenvatting,
            ToonPrimaireActies: !toonKastFormulier && !toonApparaatFormulier,
            ToonWandOpstelling: samenvatting.Kasten.Count > 0 || samenvatting.Apparaten.Count > 0,
            Werkstap: werkstap);
    }

    private static List<T> ZoekOpVolgorde<T>(IEnumerable<Guid> ids, IReadOnlyDictionary<Guid, T> lookup)
        where T : class
        => ids
            .Select(lookup.GetValueOrDefault)
            .Where(item => item is not null)
            .Cast<T>()
            .ToList();
}

public sealed record KastenInvoerPaginaModel(
    KastenInvoerActieveWerkruimteModel? ActieveWerkruimte);

public sealed record KastenInvoerActieveWerkruimteModel(
    KastenInvoerWandSamenvatting Samenvatting,
    bool ToonPrimaireActies,
    bool ToonWandOpstelling,
    KastenInvoerWerkstapModel Werkstap);

public sealed record KastenInvoerWerkstapModel(
    string Kicker,
    string? Titel,
    string Beschrijving);

public sealed record KastenInvoerWandSamenvatting(
    KeukenWand Wand,
    IReadOnlyList<Kast> Kasten,
    IReadOnlyList<Apparaat> Apparaten,
    double GevuldeBreedte,
    double VrijeBreedte);
