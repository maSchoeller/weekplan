namespace Weekplan.Core.Wochenplanung.Contracts;

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
        new("Di", "Dienstag", "Büro"),
        new("Mi", "Mittwoch", "Büro"),
        new("Do", "Donnerstag", "Büro"),
        new("Fr", "Freitag", "Homeoffice"),
        new("Sa", "Samstag", "—"),
        new("So", "Sonntag", "—")
    ];

    public static IReadOnlyList<Mahlzeit> Mahlzeiten { get; } =
    [
        new("fruehstueck", "Frühstück", 0.32),
        new("mittag", "Mittag", 0.38),
        new("abend", "Abend", 0.30)
    ];
}
