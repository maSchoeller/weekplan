namespace Weekplan.Core.Stammdaten.Contracts;

/// <summary>
/// Ein Rezept, wie es hereinkommt — ohne Kennung. Die vergibt der Slice aus dem
/// Namen, damit sie lesbar bleibt und niemand sie erfinden muss.
/// </summary>
public sealed record Rezeptentwurf(
    string Name,
    string Kategorie,
    int ZeitMin,
    bool Kalt,
    int Kcal,
    int Protein,
    IReadOnlyList<Zutat> Zutaten,
    string Anleitung);

/// <summary>
/// Das Rezept haelt die Regeln nicht ein. Die Meldung zaehlt **alle** Verstoesse
/// auf und nennt bei Kategorie und Abteilung die erlaubten Werte — der Aufrufer
/// ist ein Sprachmodell und soll nicht raten muessen, sondern korrigieren
/// koennen.
/// </summary>
public sealed class RezeptUngueltigException(IReadOnlyList<string> klagen)
    : Exception(string.Join(" ", klagen))
{
    public IReadOnlyList<string> Klagen { get; } = klagen;

    public RezeptUngueltigException(string klage) : this([klage]) { }
}
