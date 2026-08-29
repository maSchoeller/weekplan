using System.Security.Cryptography;
using System.Text.Json;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Server;

/// <summary>
/// Die fertige Antwort auf <c>GET /stammdaten</c>, einmal gebaut und im
/// Speicher gehalten: dieselben Bytes fuer alle, dasselbe ETag. Der Client legt
/// die Antwort im Browser ab und fragt danach nur noch, ob sich das Kennzeichen
/// geaendert hat — beim Kaltstart des Servers spart das den Umweg ueber die
/// Datenbank, und im Alltag die ganze Uebertragung.
///
/// <para>
/// <see cref="Verwerfen"/> ruft jedes schreibende MCP-Werkzeug. Ohne das saehe
/// die App ein neu angelegtes Rezept erst nach einem Neustart des Servers — und
/// niemand verstuende, warum es nicht da ist.
/// </para>
/// </summary>
public sealed class Stammdatenausgabe(IStammdaten quelle)
{
    private static readonly JsonSerializerOptions Format = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _tor = new(1, 1);
    private Stand? _stand;

    public sealed record Stand(string ETag, byte[] Json);

    public async Task<Stand> StandAsync(CancellationToken ct)
    {
        if (_stand is { } fertig) return fertig;

        await _tor.WaitAsync(ct);
        try
        {
            // Zweite Pruefung im Tor: waehrend des Wartens kann ein anderer
            // Aufruf den Stand schon gebaut haben.
            if (_stand is { } inzwischen) return inzwischen;

            var daten = await quelle.AllesAsync(ct);
            var json = JsonSerializer.SerializeToUtf8Bytes(daten, Format);
            return _stand = new Stand(Kennzeichen(json), json);
        }
        finally
        {
            _tor.Release();
        }
    }

    public void Verwerfen() => _stand = null;

    /// <summary>
    /// Ein starkes ETag: der halbe SHA-256 der Antwort. Es haengt nur am Inhalt,
    /// also bekommen zwei Server-Instanzen mit denselben Daten dasselbe
    /// Kennzeichen — ein Zeitstempel taete das nicht.
    /// </summary>
    private static string Kennzeichen(byte[] json)
        => $"\"{Convert.ToHexString(SHA256.HashData(json))[..16].ToLowerInvariant()}\"";
}
