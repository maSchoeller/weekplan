namespace Weekplan.Core.Stammdaten;

/// <summary>
/// Die Naht zwischen dem Slice und dem, worauf er ablegt — dieselbe Form wie
/// beim Tagebuch, nur mit <c>art</c> statt der Nutzerkennung als Partition. Die
/// Stammdaten gehoeren keinem Nutzer.
///
/// <para>
/// <c>AlleAsync</c> gibt es hier zusaetzlich: die Rezepte liegen als je eigenes
/// Dokument, und der Start braucht sie alle auf einmal. In Cosmos ist das eine
/// Abfrage innerhalb einer Partition.
/// </para>
/// </summary>
internal interface IAblage
{
    Task<T?> LesenAsync<T>(string art, string id, CancellationToken ct) where T : class;

    Task<IReadOnlyList<T>> AlleAsync<T>(string art, CancellationToken ct) where T : class;

    Task SchreibenAsync<T>(string art, string id, T inhalt, CancellationToken ct) where T : class;

    /// <returns><c>false</c>, wenn es nichts zu loeschen gab.</returns>
    Task<bool> LoeschenAsync(string art, string id, CancellationToken ct);
}
