namespace Keuken_inmeten.Models;

public sealed record BestellijstPdfPayload(
    string Titel,
    string GeneratedAtLabel,
    string PaneelType,
    string DikteLabel,
    string CncReferentieLabel,
    string TotaalOppervlakteLabel,
    int Orderregels,
    int TotaalAantal,
    int TotaalBoorgaten,
    IReadOnlyList<BestellijstPdfRegelPayload> Regels);

public sealed record BestellijstPdfRegelPayload(
    int RegelNummer,
    string RegelCode,
    string Naam,
    int Aantal,
    string PaneelMeta,
    IReadOnlyList<string> BronLocaties,
    string ZaagmaatLabel,
    string OppervlaktePerStukLabel,
    string TotaleOppervlakteLabel,
    string MateriaalLabel,
    string BoorbeeldSamenvatting,
    string CncReferentieLabel,
    string GeenBoorwerkTekst,
    IReadOnlyList<BestellijstPdfBoorgatPayload> Boorgaten,
    string VisualSvg);

public sealed record BestellijstPdfBoorgatPayload(
    int Nummer,
    string XCncLabel,
    string YCncLabel);
