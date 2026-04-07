namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;

/// <summary>
/// Berekent een zaagplan: verdeelt panelen over platen met een shelf-based bin-packing algoritme (FFDH).
/// Optioneel mag een paneel 90° gedraaid worden als het dan beter past.
/// </summary>
public static class ZaagplanService
{
    public const double StandaardZaagbreedte = 4.0;

    /// <summary>
    /// Bereken het zaagplan voor de gegeven panelen op platen van de opgegeven afmetingen.
    /// </summary>
    public static ZaagplanResultaat Bereken(
        IReadOnlyList<ZaagplanPaneel> panelen,
        double plaatBreedte,
        double plaatHoogte,
        double zaagbreedte = StandaardZaagbreedte,
        bool draaiToe = true)
    {
        var resultaat = new ZaagplanResultaat { Zaagbreedte = zaagbreedte };

        // Expand: elk paneel × aantal, en sorteer aflopend op hoogte (FFDH)
        var stukken = ExpandEnSorteer(panelen, plaatBreedte, plaatHoogte, draaiToe);

        var platen = new List<PlaatState>();

        foreach (var stuk in stukken)
        {
            if (!PastOpPlaat(stuk.Breedte, stuk.Hoogte, plaatBreedte, plaatHoogte))
            {
                // Probeer gedraaid
                if (draaiToe && PastOpPlaat(stuk.Hoogte, stuk.Breedte, plaatBreedte, plaatHoogte))
                {
                    PlaatsStuk(platen, stuk.Naam, stuk.Hoogte, stuk.Breedte, true,
                        plaatBreedte, plaatHoogte, zaagbreedte);
                }
                else
                {
                    resultaat.NietGeplaatst.Add(new ZaagplanPaneel
                    {
                        Naam = stuk.Naam,
                        Hoogte = stuk.Hoogte,
                        Breedte = stuk.Breedte,
                        Aantal = 1
                    });
                }
                continue;
            }

            PlaatsStuk(platen, stuk.Naam, stuk.Breedte, stuk.Hoogte, false,
                plaatBreedte, plaatHoogte, zaagbreedte);
        }

        for (int i = 0; i < platen.Count; i++)
        {
            platen[i].Plaat.Nummer = i + 1;
            resultaat.Platen.Add(platen[i].Plaat);
        }

        // Groepeer niet-geplaatste panelen
        resultaat.NietGeplaatst = GroepeerNietGeplaatst(resultaat.NietGeplaatst);

        return resultaat;
    }

    /// <summary>
    /// Bouw panelen uit de bestellijst-items (elke orderregel × aantal).
    /// </summary>
    public static List<ZaagplanPaneel> VanBestellijst(IReadOnlyList<BestellijstItem> items)
    {
        var panelen = new List<ZaagplanPaneel>();
        foreach (var item in items)
        {
            panelen.Add(new ZaagplanPaneel
            {
                Naam = item.Naam,
                Hoogte = item.Hoogte,
                Breedte = item.Breedte,
                Aantal = item.Aantal
            });
        }
        return panelen;
    }

    private static bool PastOpPlaat(double breedte, double hoogte, double plaatBreedte, double plaatHoogte)
        => breedte <= plaatBreedte && hoogte <= plaatHoogte;

    private static void PlaatsStuk(
        List<PlaatState> platen,
        string naam,
        double breedte,
        double hoogte,
        bool gedraaid,
        double plaatBreedte,
        double plaatHoogte,
        double zaagbreedte)
    {
        // Probeer op een bestaande plaat te plaatsen (first fit)
        foreach (var staat in platen)
        {
            if (ProbeerOpPlaat(staat, naam, breedte, hoogte, gedraaid, plaatBreedte, plaatHoogte, zaagbreedte))
                return;
        }

        // Nieuwe plaat nodig
        var nieuwePlaat = new PlaatState(plaatBreedte, plaatHoogte);
        platen.Add(nieuwePlaat);
        ProbeerOpPlaat(nieuwePlaat, naam, breedte, hoogte, gedraaid, plaatBreedte, plaatHoogte, zaagbreedte);
    }

    private static bool ProbeerOpPlaat(
        PlaatState staat,
        string naam,
        double breedte,
        double hoogte,
        bool gedraaid,
        double plaatBreedte,
        double plaatHoogte,
        double zaagbreedte)
    {
        // Probeer het stuk op een bestaande shelf te plaatsen
        foreach (var shelf in staat.Shelves)
        {
            if (hoogte <= shelf.Hoogte && shelf.GebruikteBreedte + breedte <= plaatBreedte)
            {
                var plaatsing = new ZaagplanPlaatsing
                {
                    Naam = naam,
                    X = shelf.GebruikteBreedte,
                    Y = shelf.Y,
                    Breedte = breedte,
                    Hoogte = hoogte,
                    Gedraaid = gedraaid
                };
                staat.Plaat.Plaatsingen.Add(plaatsing);
                shelf.GebruikteBreedte += breedte + zaagbreedte;
                return true;
            }
        }

        // Maak een nieuwe shelf als er verticale ruimte is
        var nieuweShelfY = staat.GebruikteHoogte;
        if (nieuweShelfY + hoogte <= plaatHoogte)
        {
            var shelf = new ShelfState
            {
                Y = nieuweShelfY,
                Hoogte = hoogte,
                GebruikteBreedte = 0
            };
            staat.Shelves.Add(shelf);
            staat.GebruikteHoogte = nieuweShelfY + hoogte + zaagbreedte;

            var plaatsing = new ZaagplanPlaatsing
            {
                Naam = naam,
                X = 0,
                Y = shelf.Y,
                Breedte = breedte,
                Hoogte = hoogte,
                Gedraaid = gedraaid
            };
            staat.Plaat.Plaatsingen.Add(plaatsing);
            shelf.GebruikteBreedte = breedte + zaagbreedte;
            return true;
        }

        return false;
    }

    private sealed class PlaatState(double breedte, double hoogte)
    {
        public ZaagplanPlaat Plaat { get; } = new() { Breedte = breedte, Hoogte = hoogte };
        public List<ShelfState> Shelves { get; } = [];
        public double GebruikteHoogte { get; set; }
    }

    private sealed class ShelfState
    {
        public double Y { get; set; }
        public double Hoogte { get; set; }
        public double GebruikteBreedte { get; set; }
    }

    private sealed record StukInfo(string Naam, double Breedte, double Hoogte);

    private static List<StukInfo> ExpandEnSorteer(
        IReadOnlyList<ZaagplanPaneel> panelen,
        double plaatBreedte,
        double plaatHoogte,
        bool draaiToe)
    {
        var stukken = new List<StukInfo>();
        foreach (var paneel in panelen)
        {
            for (int i = 0; i < paneel.Aantal; i++)
            {
                var b = paneel.Breedte;
                var h = paneel.Hoogte;

                // Oriënteer zo dat hoogte ≥ breedte (langste kant verticaal),
                // tenzij het niet past en draaien wel helpt.
                if (draaiToe && h < b)
                {
                    // Draai als hoogte < breedte, zodat de langste kant als hoogte dient
                    // Maar alleen als het gedraaid ook op de plaat past
                    if (b <= plaatHoogte && h <= plaatBreedte)
                        (b, h) = (h, b);
                }

                stukken.Add(new StukInfo(paneel.Naam, b, h));
            }
        }

        // Sorteer aflopend op hoogte (FFDH strategie)
        stukken.Sort((a, b) =>
        {
            var cmp = b.Hoogte.CompareTo(a.Hoogte);
            return cmp != 0 ? cmp : b.Breedte.CompareTo(a.Breedte);
        });

        return stukken;
    }

    private static List<ZaagplanPaneel> GroepeerNietGeplaatst(List<ZaagplanPaneel> panelen)
    {
        if (panelen.Count == 0) return panelen;

        var groepen = panelen
            .GroupBy(p => $"{p.Naam}|{p.Hoogte:0.###}|{p.Breedte:0.###}")
            .Select(g => new ZaagplanPaneel
            {
                Naam = g.First().Naam,
                Hoogte = g.First().Hoogte,
                Breedte = g.First().Breedte,
                Aantal = g.Count()
            })
            .ToList();

        return groepen;
    }
}
