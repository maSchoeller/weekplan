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
}
