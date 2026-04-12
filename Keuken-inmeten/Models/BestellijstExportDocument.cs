namespace Keuken_inmeten.Models;

public sealed record BestellijstExportDocument(
    string Titel,
    string PaneelType,
    string DikteMm,
    DateTime GeneratedAt,
    string CncNulpuntLabel,
    string CncXAsLabel,
    string CncYAsLabel,
    int Orderregels,
    int TotaalAantal,
    int TotaalBoorgaten,
    int MaxBoorgaten,
    double TotaalOppervlakteM2,
    IReadOnlyList<BestellijstExportRegel> Regels);

public sealed record BestellijstExportRegel(
    int RegelNummer,
    string RegelCode,
    string Naam,
    int Aantal,
    string PaneelRolLabel,
    string ScharnierLabel,
    double HoogteMm,
    double BreedteMm,
    double OppervlaktePerStukM2,
    double TotaleOppervlakteM2,
    string KantenbandLabel,
    string ContextLabel,
    IReadOnlyList<string> BronLocaties,
    IReadOnlyList<BestellijstExportBoorgat> Boorgaten,
    BestellijstVisualDocument Visual);

public sealed record BestellijstExportBoorgat(
    int Nummer,
    double XCncMm,
    double YCncMm);

public sealed record BestellijstVisualDocument(
    string RegelCode,
    double BreedteMm,
    double HoogteMm,
    ScharnierZijde ScharnierZijde,
    IReadOnlyList<BestellijstVisualBoorgat> Boorgaten);

public sealed record BestellijstVisualBoorgat(
    double XVanScharnierzijdeMm,
    double YVanafBovenMm,
    double DiameterMm);
