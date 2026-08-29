namespace Weekplan.Core.Stammdaten.Contracts;

/// <summary>
/// Der Zugang zu den festen Daten. Lesen ist der Normalfall; geschrieben wird
/// aus dem Gespraech ueber den MCP-Endpunkt — seit dem Lauf 2026-08-29 nicht
/// mehr nur die Rezepte, sondern auch Trainingsplan, Grundstock und
/// Abteilungen. Jedes Schreiben ersetzt sein Dokument vollstaendig; es gibt
/// bewusst keine Teilaenderung, weil sonst zwei Denkweisen nebeneinander
/// stuenden.
/// </summary>
public interface IStammdaten
{
    Task<Stammdatensatz> AllesAsync(CancellationToken ct = default);

    Task<Rezept?> RezeptAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Schreibt alles Uebergebene und ueberschreibt Gleichnamiges. Entfernt
    /// nichts: was in der Datenbank steht und hier fehlt, bleibt liegen.
    /// </summary>
    Task BefuellenAsync(Stammdatensatz daten, CancellationToken ct = default);

    /// <summary>
    /// Legt ein Rezept an. Die Kennung entsteht aus dem Namen; gibt es sie
    /// schon, ist das ein Fehler und kein stilles Ueberschreiben.
    /// </summary>
    /// <exception cref="StammdatenUngueltigException">Das Rezept haelt die Regeln nicht ein.</exception>
    Task<Rezept> AnlegenAsync(Rezeptentwurf entwurf, CancellationToken ct = default);

    /// <summary>Ersetzt ein vorhandenes Rezept. Es muss vorhanden sein — Aendern legt nicht an.</summary>
    /// <exception cref="StammdatenUngueltigException">Unbekannte Kennung, oder das Rezept haelt die Regeln nicht ein.</exception>
    Task<Rezept> AendernAsync(string id, Rezeptentwurf entwurf, CancellationToken ct = default);

    /// <returns><c>false</c>, wenn es das Rezept nicht gab.</returns>
    Task<bool> LoeschenAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Ersetzt den Trainingsplan. Das Regelwerk bleibt stehen — es steht nicht
    /// im <see cref="Trainingsentwurf"/> und laesst sich darum nicht schreiben.
    /// </summary>
    /// <exception cref="StammdatenUngueltigException">Der Plan haelt die Regeln nicht ein.</exception>
    Task<Trainingsdaten> TrainingSchreibenAsync(Trainingsentwurf entwurf, CancellationToken ct = default);

    /// <exception cref="StammdatenUngueltigException">Der Grundstock haelt die Regeln nicht ein.</exception>
    Task<Grundstockdaten> GrundstockSchreibenAsync(Grundstockdaten daten, CancellationToken ct = default);

    /// <summary>
    /// Ersetzt die Abteilungsliste. Zutaten, deren Abteilung dabei wegfaellt,
    /// wandern in die Sammelabteilung am Ende der Liste — kein Rezept wird
    /// ungueltig, keine Zutat verschwindet. Wie viele es waren, sagt die
    /// Rueckgabe.
    /// </summary>
    /// <exception cref="StammdatenUngueltigException">Die Liste haelt die Regeln nicht ein.</exception>
    Task<Abteilungsumzug> AbteilungenSchreibenAsync(
        Abteilungsentwurf entwurf, CancellationToken ct = default);
}
