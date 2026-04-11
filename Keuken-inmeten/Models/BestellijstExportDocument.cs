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
    IReadOnlyList<BestellijstExportRegel> Regels);

public sealed record BestellijstExportRegel(
    string Naam,
    int Aantal,
    string PaneelRolLabel,
    double HoogteMm,
    double BreedteMm,
    string KantenbandLabel,
    string ContextLabel,
    IReadOnlyList<BestellijstExportBoorgat> Boorgaten,
    BestellijstVisualDocument Visual);

public sealed record BestellijstExportBoorgat(
    int Nummer,
    double XCncMm,
    double YCncMm);

public sealed record BestellijstVisualDocument(
    double BreedteMm,
    double HoogteMm,
    ScharnierZijde ScharnierZijde,
    IReadOnlyList<BestellijstVisualBoorgat> Boorgaten);

public sealed record BestellijstVisualBoorgat(
    double XVanScharnierzijdeMm,
    double YVanafBovenMm,
    double DiameterMm);
