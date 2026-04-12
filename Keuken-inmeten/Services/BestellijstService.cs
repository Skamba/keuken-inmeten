namespace Keuken_inmeten.Services;

using System.Globalization;
using Keuken_inmeten.Models;

public static class BestellijstService
{
    public const string StandaardKantenbandLabel = "kantenband rondom";

    private sealed record BestellijstBron(
        string BasisNaam,
        string ContextLabel,
        Guid? WandId,
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
            var toewijzing = state.Toewijzingen.Find(t => t.Id == resultaat.ToewijzingId);
            var kasten = state.ZoekKasten(resultaat.KastIds.ToList());
            var wand = resultaat.KastIds
                .Select(kastId => state.WandVoorKast(kastId))
                .FirstOrDefault(kandidaat => kandidaat is not null);
            var wandNaam = string.IsNullOrWhiteSpace(wand?.Naam) ? "Onbekende wand" : wand.Naam;
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
                wand?.Id,
                wandNaam,
                kastenLabel,
                BepaalPaneelRolLabel(resultaat.Type),
                scharnierLabel,
                BepaalSignatuur(resultaat, basisNaam, toewijzing, wand?.Id),
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
                    KantenbandLabel = StandaardKantenbandLabel,
                    PaneelRolLabel = eerste.PaneelRolLabel,
                    WandId = eerste.WandId,
                    WandNaam = eerste.WandNaam,
                    KastenLabel = eerste.KastenLabel,
                    ContextLabel = contextLabel,
                    BronLocaties = [.. contexten],
                    ScharnierLabel = eerste.ScharnierLabel,
                    Hoogte = eerste.Resultaat.Hoogte,
                    Breedte = eerste.Resultaat.Breedte,
                    Boorgaten = eerste.Resultaat.Boorgaten
                        .OrderBy(boorgat => boorgat.Y)
                        .ThenBy(boorgat => boorgat.X)
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
            .OrderByDescending(item => item.Boorgaten.Count > 0)
            .ThenByDescending(item => item.Hoogte * item.Breedte)
            .ThenByDescending(item => item.Hoogte)
            .ThenByDescending(item => item.Breedte)
            .ThenBy(item => item.BasisNaam, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Naam, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return gegroepeerd;
    }

    private static string BepaalBasisNaam(PaneelResultaat resultaat, List<Kast> kasten)
        => resultaat.Type switch
        {
            PaneelType.Deur => BepaalDeurNaam(resultaat, kasten),
            PaneelType.LadeFront => "Paneel",
            PaneelType.BlindPaneel => "Paneel",
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

    private static string BepaalSignatuur(PaneelResultaat resultaat, string basisNaam, PaneelToewijzing? toewijzing, Guid? wandId)
    {
        var genormaliseerdType = BepaalGroeperingsType(resultaat.Type);
        var scharnierSignatuur = resultaat.Type == PaneelType.Deur
            ? resultaat.ScharnierZijde.ToString()
            : string.Empty;

        return string.Join("|",
            basisNaam,
            genormaliseerdType,
            Fmt(resultaat.Breedte),
            Fmt(resultaat.Hoogte),
            scharnierSignatuur,
            toewijzing is null ? string.Empty : BepaalGroeperingsType(toewijzing.Type));
    }

    private static string BepaalPaneelRolLabel(PaneelType type)
        => type == PaneelType.Deur ? VisualisatieHelper.PaneelTypeLabel(type) : "Paneel";

    private static string BepaalGroeperingsType(PaneelType type)
        => type == PaneelType.Deur ? type.ToString() : "Paneel";

    private static string Fmt(double value) => VisualisatieHelper.FmtData(value);
}
