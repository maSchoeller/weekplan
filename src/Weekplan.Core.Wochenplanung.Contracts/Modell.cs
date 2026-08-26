namespace Weekplan.Core.Wochenplanung.Contracts;

/// <summary>
/// Eine Zutat mit Menge je Portion. <paramref name="Vorrat"/> markiert, was im
/// Grundstock steht und deshalb nicht auf die Wochenliste gehoert — 6 g
/// Olivenoel sind rechnerisch richtig und praktisch Unsinn.
/// </summary>
public sealed record Zutat(string Name, double G, string Abt, bool Vorrat = false, double Stk = 0);

public sealed record Rezept(
    string Id,
    string Name,
    string Kategorie,
    int ZeitMin,
    bool Kalt,
    int Kcal,
    int Protein,
    IReadOnlyList<Zutat> Zutaten,
    IReadOnlyList<string> Schritte);

/// <summary>Ein Posten der Einkaufsliste, ueber alle Gerichte der Woche summiert.</summary>
public sealed record Einkaufsposten(
    string Name,
    string Abteilung,
    double Gramm,
    double Stueck,
    IReadOnlyList<string> Quellen)
{
    /// <summary>Eier zaehlt man in Stueck, alles andere wiegt man.</summary>
    public bool InStueck => Stueck > 0;
}

public sealed record Einkaufsliste(IReadOnlyList<Einkaufsposten> Posten, int VorratUebersprungen);

public sealed record Wochentag(string Kuerzel, string Name, string Ort);

public sealed record Mahlzeit(string Schluessel, string Beschriftung, double Anteil);

public static class Woche
{
    public static IReadOnlyList<Wochentag> Tage { get; } =
    [
        new("Mo", "Montag", "Homeoffice"),
        new("Di", "Dienstag", "Buero"),
        new("Mi", "Mittwoch", "Buero"),
        new("Do", "Donnerstag", "Buero"),
        new("Fr", "Freitag", "Homeoffice"),
        new("Sa", "Samstag", "—"),
        new("So", "Sonntag", "—")
    ];

    public static IReadOnlyList<Mahlzeit> Mahlzeiten { get; } =
    [
        new("fruehstueck", "Fruehstueck", 0.32),
        new("mittag", "Mittag", 0.38),
        new("abend", "Abend", 0.30)
    ];
}
