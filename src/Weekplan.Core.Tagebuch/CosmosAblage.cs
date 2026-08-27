using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Weekplan.Core.Tagebuch;

/// <summary>
/// Dokumente in einem Cosmos-Container: die <c>id</c> ist der Name, der
/// Partitionsschluessel die Nutzerkennung. Genau die Form, die
/// <see cref="IAblage"/> beschreibt — ein Dokument je (Nutzer, Name) —, also
/// ist jeder Zugriff ein Punktlesen bzw. ein Upsert innerhalb einer Partition
/// und kostet einstellige RU. In Azure tritt sie an die Stelle der
/// <see cref="DateiAblage"/>.
/// </summary>
internal sealed class CosmosAblage : IAblage, IDisposable
{
    private readonly CosmosClient _kunde;
    private readonly Container _behaelter;

    public CosmosAblage(string verbindung, string datenbank, string behaelter)
    {
        // Ohne diese Zeile serialisiert das SDK mit Newtonsoft, und Newtonsoft
        // macht aus einem DateOnly ein Objekt statt eines Datums. Zieltermin und
        // Gewichtsverlauf kaemen dann verstuemmelt zurueck.
        _kunde = new CosmosClient(verbindung, new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
            ApplicationName = "weekplan"
        });
        _behaelter = _kunde.GetContainer(datenbank, behaelter);
    }

    public async Task<T?> LesenAsync<T>(string nutzerId, string name, CancellationToken ct) where T : class
    {
        Pruefen(nutzerId, name);

        try
        {
            var antwort = await _behaelter.ReadItemAsync<Huelle<T>>(
                name, new PartitionKey(nutzerId), cancellationToken: ct);
            return antwort.Resource.Inhalt;
        }
        catch (CosmosException fehler) when (fehler.StatusCode == HttpStatusCode.NotFound)
        {
            // Nichts da ist kein Fehler — die Naht verspricht dafuer null.
            return null;
        }
    }

    public async Task SchreibenAsync<T>(string nutzerId, string name, T inhalt, CancellationToken ct) where T : class
    {
        Pruefen(nutzerId, name);

        await _behaelter.UpsertItemAsync(
            new Huelle<T>(name, nutzerId, inhalt), new PartitionKey(nutzerId), cancellationToken: ct);
    }

    public void Dispose() => _kunde.Dispose();

    // Die Nutzerkennung kommt aus einer Eingabe. Cosmos verbietet in einer id
    // '/', '\', '#' und '?'; die Dateiablage weist dieselben Namen ab. Beide
    // Ablagen muessen sich hier gleich verhalten, sonst haengt das Verhalten
    // der App daran, wo sie gerade laeuft.
    private static void Pruefen(string nutzerId, string name)
    {
        Feld(nutzerId, nameof(nutzerId));
        Feld(name, nameof(name));
    }

    private static void Feld(string wert, string feld)
    {
        if (string.IsNullOrWhiteSpace(wert)
            || wert.AsSpan().IndexOfAny('/', '\\', '#') >= 0
            || wert.Contains('?')
            || wert.Any(char.IsControl))
        {
            throw new ArgumentException($"Unzulaessiger Name: '{wert}'.", feld);
        }
    }
}

/// <summary>
/// Cosmos verlangt <c>id</c> und den Partitionsschluessel auf der obersten
/// Ebene des Dokuments. <see cref="IAblage"/> kennt aber nur ein beliebiges
/// <typeparamref name="T"/> und koennte dessen Felder nicht flach danebenlegen,
/// ohne fuer jeden Typ etwas zu wissen. Darum haengt der Inhalt eine Ebene
/// tiefer — der Preis dafuer, dass die Naht schmal bleibt.
/// </summary>
internal sealed record Huelle<T>(string Id, string NutzerId, T Inhalt);
