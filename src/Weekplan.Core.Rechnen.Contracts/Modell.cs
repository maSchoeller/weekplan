namespace Weekplan.Core.Rechnen.Contracts;

/// <summary>Die Werte des Nutzers, aus denen sich die Rechnung ergibt.</summary>
/// <param name="TempoKgProWoche">Eigenes Tempo; <c>null</c> = das Defizit kommt aus der Phase.</param>
public sealed record Profil(
    double GewichtKg,
    double ZielKg,
    double GroesseCm,
    int Alter,
    double ProteinFaktor,
    double? TempoKgProWoche);

/// <summary>Eine Trainingseinheit: Art und Dauer.</summary>
public sealed record Einheit(string Typ, int Minuten);

/// <summary>MET-Wert einer Trainingsart, mit Beschriftung fuer die Oberflaeche.</summary>
public sealed record MetWert(string Label, double Met);

public sealed record Trainingstag(string Tag, string Ort, IReadOnlyList<Einheit> Einheiten);

public sealed record Phase(string Id, string Name, int DefizitZiel, IReadOnlyList<Trainingstag> Tage);

/// <summary>Was ein Trainingstag netto verbraucht.</summary>
public sealed record TagesVerbrauch(string Tag, string Ort, IReadOnlyList<Einheit> Einheiten, int Kcal);

public sealed record Sportwoche(IReadOnlyList<TagesVerbrauch> ProTag, int Woche);

public sealed record Gewichtseintrag(DateOnly Datum, double Kg);

/// <summary>Das vollstaendige Rechenergebnis fuer einen Profilstand.</summary>
public sealed record Bilanz(
    int Grundumsatz,
    int Alltag,
    int SportSchnitt,
    int Gesamt,
    int Defizit,
    int PhasenDefizit,
    bool EigenesTempo,
    int Normal,
    int Refeed,
    int Protein,
    int Wochendefizit,
    double KgProWoche,
    double PhasenTempo,
    Sportwoche Sport);
