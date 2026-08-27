using System.Text.Json.Serialization;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Wochenplanung.Contracts;

namespace Weekplan.Client.Daten;

/// <summary>
/// Die festen Daten des Projekts: Rezepte, Trainingsphasen, Grundstock. Sie
/// gehoeren nicht dem Nutzer, sind fuer alle gleich und aendern sich nur durch
/// einen Commit — deshalb liegen sie als Dateien beim Client und nicht in der
/// Datenbank.
/// </summary>
public sealed record Stammdaten(Rezeptdaten Rezepte, Trainingsdaten Training, Grundstockdaten Grundstock);

public sealed record Rezeptdaten(string Hinweis, IReadOnlyList<string> Abteilungen, IReadOnlyList<Rezept> Rezepte);

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
