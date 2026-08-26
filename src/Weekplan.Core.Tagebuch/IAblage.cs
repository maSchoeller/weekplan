namespace Weekplan.Core.Tagebuch;

/// <summary>
/// Die Naht zwischen dem Slice und dem, worauf er ablegt. Ein Dokument je
/// (Nutzer, Name) — genau die Form, die Cosmos mit Partitionsschluessel und
/// <c>id</c> ohnehin hat.
/// </summary>
internal interface IAblage
{
    Task<T?> LesenAsync<T>(string nutzerId, string name, CancellationToken ct) where T : class;

    Task SchreibenAsync<T>(string nutzerId, string name, T inhalt, CancellationToken ct) where T : class;
}
