namespace Weekplan.Core.Stammdaten.Contracts;

/// <summary>
/// Der Zugang zu den festen Daten. Lesen ist der Normalfall — geschrieben wird
/// bisher nur einmal, von der Erstbefuellung.
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
    /// <exception cref="RezeptUngueltigException">Das Rezept haelt die Regeln nicht ein.</exception>
    Task<Rezept> AnlegenAsync(Rezeptentwurf entwurf, CancellationToken ct = default);

    /// <summary>Ersetzt ein vorhandenes Rezept. Es muss vorhanden sein — Aendern legt nicht an.</summary>
    /// <exception cref="RezeptUngueltigException">Unbekannte Kennung, oder das Rezept haelt die Regeln nicht ein.</exception>
    Task<Rezept> AendernAsync(string id, Rezeptentwurf entwurf, CancellationToken ct = default);

    /// <returns><c>false</c>, wenn es das Rezept nicht gab.</returns>
    Task<bool> LoeschenAsync(string id, CancellationToken ct = default);
}
