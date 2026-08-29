using System.Text.Json.Serialization;
using Weekplan.Core.Rechnen.Contracts;

namespace Weekplan.Core.Stammdaten.Contracts;

/// <summary>
/// Eine Zutat mit Menge je Portion. <paramref name="Vorrat"/> markiert, was im
/// Grundstock steht und deshalb nicht auf die Wochenliste gehoert — 6 g
/// Olivenoel sind rechnerisch richtig und praktisch Unsinn.
/// </summary>
public sealed record Zutat(string Name, double G, string Abt, bool Vorrat = false, double Stk = 0);

/// <summary>
/// Ein Rezept. <paramref name="Anleitung"/> ist Markdown und hat die frueheren
/// Einzelschritte abgeloest: eine Anleitung, nach der man ein Gericht auch beim
/// ersten Mal kocht, braucht Zwischenueberschriften und Fliesstext, keine Liste
/// aus fuenf Saetzen.
/// </summary>
public sealed record Rezept(
    string Id,
    string Name,
    string Kategorie,
    int ZeitMin,
    bool Kalt,
    bool Prep,
    int Kcal,
    int Protein,
    IReadOnlyList<Zutat> Zutaten,
    string Anleitung);

/// <summary>
/// Die erlaubten Kategorien eines Rezepts. Sie stehen hier und nicht bei der
/// Wochenplanung, weil die Pruefung eines geschriebenen Rezepts sie braucht und
/// ein Zugriff auf <c>Woche.Mahlzeiten</c> die beiden Slices zum Ring schliessen
/// wuerde. <c>KategorienTests</c> haelt beide Listen zusammen.
/// </summary>
public static class Kategorien
{
    public static IReadOnlyList<string> Erlaubt { get; } = ["fruehstueck", "mittag", "abend"];
}

public sealed record Rezeptdaten(string Hinweis, IReadOnlyList<string> Abteilungen, IReadOnlyList<Rezept> Rezepte);

/// <summary>Nur der Kopf der Rezeptdaten — das eigene Dokument neben den Rezepten.</summary>
public sealed record Abteilungsdaten(string Hinweis, IReadOnlyList<string> Abteilungen);

public sealed record Trainingsdaten(
    string Hinweis,
    IReadOnlyDictionary<string, MetWert> MetWerte,
    IReadOnlyList<PhasenAnzeige> Phasen,
    Kraftplan Kraftplan,
    IReadOnlyList<Regel> Regeln);

/// <summary>Eine Phase mit allem, was die Oberflaeche zeigt — die Rechnung braucht davon nur einen Teil.</summary>
public sealed record PhasenAnzeige(
    string Id,
    string Name,
    string Wochen,
    string Zeitraum,
    int DefizitZiel,
    string Beschreibung,
    IReadOnlyList<TrainingstagDaten> Tage)
{
    public Phase AlsPhase() => new(Id, Name, DefizitZiel,
        [.. Tage.Select(t => new Trainingstag(t.Tag, t.Ort, [.. t.Einheiten.Select(e => e.Als())]))]);
}

public sealed record TrainingstagDaten(string Tag, string Ort, IReadOnlyList<EinheitDaten> Einheiten);

/// <summary>In der Datei heisst die Dauer <c>min</c>; der Vertrag nennt sie ausgeschrieben.</summary>
public sealed record EinheitDaten(string Typ, [property: JsonPropertyName("min")] int Min)
{
    public Einheit Als() => new(Typ, Min);
}

public sealed record Kraftplan(string Equipment, string Prinzip, IReadOnlyList<KraftEinheit> Einheiten);

public sealed record KraftEinheit(string Id, string Name, IReadOnlyList<Uebung> Uebungen);

public sealed record Uebung(string Name, string Saetze, string Wdh, string Hinweis);

public sealed record Regel(string Titel, string Text);

public sealed record Grundstockdaten(string Hinweis, IReadOnlyList<Gruppe> Gruppen);

public sealed record Gruppe(string Name, IReadOnlyList<Artikel> Artikel);

public sealed record Artikel(string Name, string Menge, string Reichweite);

/// <summary>
/// Die festen Daten des Projekts: Rezepte, Trainingsphasen, Grundstock. Sie
/// gehoeren keinem Nutzer und sind fuer alle gleich — frueher lagen sie als
/// Dateien beim Client, seit diesem Lauf in der Datenbank.
/// </summary>
public sealed record Stammdatensatz(Rezeptdaten Rezepte, Trainingsdaten Training, Grundstockdaten Grundstock);
