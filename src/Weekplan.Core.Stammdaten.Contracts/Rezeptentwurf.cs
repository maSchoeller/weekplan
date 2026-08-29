using Weekplan.Core.Rechnen.Contracts;

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
    bool Prep,
    int Kcal,
    int Protein,
    IReadOnlyList<Zutat> Zutaten,
    string Anleitung);

/// <summary>
/// Der Trainingsplan, wie er hereinkommt — dieselben Felder wie
/// <see cref="Trainingsdaten"/>, <b>ohne die Regeln</b>. Das ist kein Versehen,
/// sondern der Schreibschutz selbst: es gibt keinen Weg, Regeln zu uebergeben,
/// also kann auch keiner sie versehentlich ueberschreiben. Der Dienst legt die
/// vorhandenen beim Schreiben zurueck.
/// </summary>
public sealed record Trainingsentwurf(
    string Hinweis,
    IReadOnlyDictionary<string, MetWert> MetWerte,
    IReadOnlyList<PhasenAnzeige> Phasen,
    Kraftplan Kraftplan);

/// <summary>Die Abteilungsliste, wie sie hereinkommt.</summary>
public sealed record Abteilungsentwurf(string Hinweis, IReadOnlyList<string> Abteilungen);

/// <summary>
/// Was das Schreiben der Abteilungen bewirkt hat. Die Zahlen sind der eigentliche
/// Schutz: wer eine Abteilung entfernt, erfaehrt im selben Atemzug, wie viele
/// Zutaten dadurch umgezogen sind.
/// </summary>
public sealed record Abteilungsumzug(Abteilungsdaten Abteilungen, int Zutaten, int Rezepte);

/// <summary>
/// Die Daten halten die Regeln nicht ein. Die Meldung zaehlt **alle** Verstoesse
/// auf und nennt die erlaubten Werte — der Aufrufer ist ein Sprachmodell und
/// soll nicht raten muessen, sondern korrigieren koennen.
/// </summary>
public sealed class StammdatenUngueltigException(IReadOnlyList<string> klagen)
    : Exception(string.Join(" ", klagen))
{
    public IReadOnlyList<string> Klagen { get; } = klagen;

    public StammdatenUngueltigException(string klage) : this([klage]) { }
}
