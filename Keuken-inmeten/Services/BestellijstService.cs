namespace Keuken_inmeten.Services;

using System.Globalization;
using Keuken_inmeten.Models;

public static class BestellijstService
{
    public const string StandaardAbsBandLabel = "1 mm ABS rondom";

    private sealed record BestellijstBron(
        string BasisNaam,
        string ContextLabel,
        string WandNaam,
        string KastenLabel,
        string PaneelRolLabel,
        string ScharnierLabel,
        string Signatuur,
        PaneelResultaat Resultaat);

    public static List<BestellijstItem> BerekenItems(KeukenStateService state)
    {
        var resultaten = state.BerekenResultaten();
        var bronnen = new List<BestellijstBron>();

        for (int i = 0; i < resultaten.Count; i++)
        {
            var resultaat = resultaten[i];
            var toewijzing = i < state.Toewijzingen.Count ? state.Toewijzingen[i] : null;
            var kasten = resultaat.KastIds
                .Select(id => state.Kasten.Find(k => k.Id == id))
                .Where(k => k is not null)
                .Cast<Kast>()
                .ToList();

            var wandNaam = resultaat.KastIds
                .Select(id => state.Wanden.Find(w => w.KastIds.Contains(id))?.Naam)
                .FirstOrDefault(naam => !string.IsNullOrWhiteSpace(naam)) ?? "Onbekende wand";
            var kastenLabel = string.Join(" + ", kasten.Select(k => k.Naam));
            var basisNaam = BepaalBasisNaam(resultaat, kasten);
            var contextParts = new List<string> { wandNaam };

            if (!string.IsNullOrWhiteSpace(kastenLabel))
                contextParts.Add(kastenLabel);

            var scharnierLabel = resultaat.Type == PaneelType.Deur
                ? resultaat.ScharnierZijde.ToString()
                : "—";
            if (resultaat.Type == PaneelType.Deur)
                contextParts.Add($"scharnier {scharnierLabel.ToLowerInvariant()}");

            var contextLabel = string.Join(" • ", contextParts);

            bronnen.Add(new BestellijstBron(
                basisNaam,
                contextLabel,
                wandNaam,
                kastenLabel,
                TypeLabel(resultaat.Type),
                scharnierLabel,
                BepaalSignatuur(resultaat, basisNaam, toewijzing),
                resultaat));
        }

        var tellers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var gegroepeerd = bronnen
            .GroupBy(bron => bron.Signatuur)
            .Select(groep =>
            {
                var eerste = groep.First();
                var contexten = groep
                    .Select(item => item.ContextLabel)
                    .Where(label => !string.IsNullOrWhiteSpace(label))
                    .Distinct()
                    .ToList();

                tellers.TryGetValue(eerste.BasisNaam, out var huidigNummer);
                huidigNummer++;
                tellers[eerste.BasisNaam] = huidigNummer;

                var contextLabel = contexten.Count switch
                {
                    0 => "",
                    1 => contexten[0],
                    _ => $"{contexten[0]} (+{contexten.Count - 1} meer)"
                };

                return new BestellijstItem
                {
                    BasisNaam = eerste.BasisNaam,
                    Naam = $"{eerste.BasisNaam} {huidigNummer}",
                    Aantal = groep.Count(),
                    AbsBandLabel = StandaardAbsBandLabel,
                    PaneelRolLabel = eerste.PaneelRolLabel,
                    WandNaam = eerste.WandNaam,
                    KastenLabel = eerste.KastenLabel,
                    ContextLabel = contextLabel,
                    ScharnierLabel = eerste.ScharnierLabel,
                    Hoogte = eerste.Resultaat.Hoogte,
                    Breedte = eerste.Resultaat.Breedte,
                    Boorgaten = eerste.Resultaat.Boorgaten
                        .OrderBy(boorgat => boorgat.Y)
                        .Select(boorgat => new Boorgat
                        {
                            X = boorgat.X,
                            Y = boorgat.Y,
                            Diameter = boorgat.Diameter,
                            Onderbouwing = boorgat.Onderbouwing
                        })
                        .ToList(),
                    Resultaat = eerste.Resultaat
                };
            })
            .OrderBy(item => item.BasisNaam, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Hoogte)
            .ThenBy(item => item.Breedte)
            .ThenBy(item => item.Naam, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return gegroepeerd;
    }

    private static string BepaalBasisNaam(PaneelResultaat resultaat, List<Kast> kasten)
        => resultaat.Type switch
        {
            PaneelType.Deur => BepaalDeurNaam(resultaat, kasten),
            PaneelType.LadeFront => resultaat.Hoogte switch
            {
                >= 320 => "Hoog Ladefront",
                <= 180 => "Laag Ladefront",
                _ => "Ladefront"
            },
            PaneelType.BlindPaneel => resultaat.Hoogte switch
            {
                >= 1200 => "Hoog Blindpaneel",
                <= 500 => "Laag Blindpaneel",
                _ => "Blindpaneel"
            },
            _ => "Paneel"
        };

    private static string BepaalDeurNaam(PaneelResultaat resultaat, List<Kast> kasten)
    {
        if (kasten.Any(kast => kast.Type == KastType.HogeKast))
            return "Hoge Deur";

        if (kasten.Count > 0 && kasten.All(kast => kast.Type == KastType.Bovenkast))
            return "Boven Deur";

        if (kasten.Count > 0 && kasten.All(kast => kast.Type == KastType.Onderkast))
            return resultaat.Hoogte <= 450 ? "Lage Deur" : "Onder Deur";

        if (resultaat.Breedte >= 900)
            return "Brede Deur";

        return "Deur";
    }

    private static string BepaalSignatuur(PaneelResultaat resultaat, string basisNaam, PaneelToewijzing? toewijzing)
    {
        var boorgaten = string.Join(";",
            resultaat.Boorgaten
                .OrderBy(boorgat => boorgat.Y)
                .Select(boorgat => $"{Fmt(boorgat.X)},{Fmt(boorgat.Y)},{Fmt(boorgat.Diameter)}"));

        return string.Join("|",
            basisNaam,
            resultaat.Type,
            Fmt(resultaat.Breedte),
            Fmt(resultaat.Hoogte),
            resultaat.ScharnierZijde,
            toewijzing?.Type.ToString() ?? "",
            boorgaten);
    }

    private static string TypeLabel(PaneelType type) => type switch
    {
        PaneelType.Deur => "Deur",
        PaneelType.LadeFront => "Ladefront",
        PaneelType.BlindPaneel => "Blind paneel",
        _ => type.ToString()
    };

    private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
