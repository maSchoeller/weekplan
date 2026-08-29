using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Weekplan.Core.Stammdaten;

/// <summary>
/// Dokumente in einem Cosmos-Container: die <c>id</c> ist der Dokumentname, der
/// Partitionsschluessel die <c>art</c>. Alle Rezepte zu lesen ist damit eine
/// Abfrage <b>innerhalb einer</b> Partition und kostet einstellige RU; ein
/// einzelnes Rezept ist ein Punktlesen.
/// </summary>
internal sealed class CosmosAblage : IAblage, IDisposable
{
    private readonly CosmosClient _kunde;
    private readonly Container _behaelter;

    public CosmosAblage(string verbindung, string datenbank, string behaelter)
    {
        // Wie beim Tagebuch: ohne diese Zeile serialisiert das SDK mit
        // Newtonsoft, und dessen Vorgaben passen nicht zu den Vertragstypen.
        _kunde = new CosmosClient(verbindung, new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
            ApplicationName = "weekplan"
        });
        _behaelter = _kunde.GetContainer(datenbank, behaelter);
    }

    public async Task<T?> LesenAsync<T>(string art, string id, CancellationToken ct) where T : class
    {
        Pruefen(art, id);

        try
        {
            var antwort = await _behaelter.ReadItemAsync<Huelle<T>>(
                id, new PartitionKey(art), cancellationToken: ct);
            return antwort.Resource.Inhalt;
        }
        catch (CosmosException fehler) when (fehler.StatusCode == HttpStatusCode.NotFound)
        {
            // Nichts da ist kein Fehler — die Naht verspricht dafuer null.
            return null;
        }
    }

    public async Task<IReadOnlyList<T>> AlleAsync<T>(string art, CancellationToken ct) where T : class
    {
        Feld(art, nameof(art));

        var alle = new List<T>();
        using var seiten = _behaelter.GetItemQueryIterator<Huelle<T>>(
            new QueryDefinition("SELECT * FROM c"),
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(art) });

        while (seiten.HasMoreResults)
        {
            foreach (var huelle in await seiten.ReadNextAsync(ct)) alle.Add(huelle.Inhalt);
        }
        return alle;
    }

    public async Task SchreibenAsync<T>(string art, string id, T inhalt, CancellationToken ct) where T : class
    {
        Pruefen(art, id);

        await _behaelter.UpsertItemAsync(
            new Huelle<T>(id, art, inhalt), new PartitionKey(art), cancellationToken: ct);
    }

    public async Task<bool> LoeschenAsync(string art, string id, CancellationToken ct)
    {
        Pruefen(art, id);

        try
        {
            await _behaelter.DeleteItemAsync<object>(id, new PartitionKey(art), cancellationToken: ct);
            return true;
        }
        catch (CosmosException fehler) when (fehler.StatusCode == HttpStatusCode.NotFound)
        {
            // Nichts da ist kein Fehler — die Naht verspricht dafuer false.
            return false;
        }
    }

    public void Dispose() => _kunde.Dispose();

    // Cosmos verbietet in einer id die Zeichen Schraegstrich, Rueckstrich,
    // Doppelkreuz und Fragezeichen; die Dateiablage weist dieselben Namen ab.
    // Beide Ablagen muessen sich hier gleich verhalten, sonst haengt das
    // Verhalten der App daran, wo sie gerade laeuft.
    private static void Pruefen(string art, string id)
    {
        Feld(art, nameof(art));
        Feld(id, nameof(id));
    }

    private static void Feld(string wert, string feld)
    {
        if (string.IsNullOrWhiteSpace(wert)
            || wert.AsSpan().IndexOfAny('/', '\\', '#') >= 0
            || wert.Contains('?')
            || wert.Contains("..")
            || wert.Any(char.IsControl))
        {
            throw new ArgumentException($"Unzulaessiger Name: {wert}", feld);
        }
    }
}

/// <summary>
/// Cosmos verlangt <c>id</c> und Partitionsschluessel auf der obersten Ebene;
/// <see cref="IAblage"/> kennt aber nur ein beliebiges <typeparamref name="T"/>.
/// Darum haengt der Inhalt eine Ebene tiefer — derselbe Preis wie beim Tagebuch.
/// </summary>
internal sealed record Huelle<T>(string Id, string Art, T Inhalt);
