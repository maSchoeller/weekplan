using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Client.Dienste;

/// <summary>Der Server war nicht erreichbar und es lag nichts im Browser.</summary>
public sealed class StammdatenNichtErreichbarException(Exception ursache)
    : Exception("Die Rezepte und Trainingsphasen konnten nicht geladen werden — der Server "
                + "war nicht erreichbar. Nach einer laengeren Pause dauert der erste Ruf "
                + "einige Sekunden.", ursache);

/// <summary>
/// Die festen Daten kommen seit dem Lauf 2026-08-28 vom Server und nicht mehr
/// als Datei mit dem Client. Damit das die App nicht langsamer macht, liegt die
/// letzte Antwort im Browserspeicher: sie wird beim Start <b>sofort</b> gezeigt,
/// und erst danach fragt der Client im Hintergrund nach, ob sich das Kennzeichen
/// geaendert hat. Der Kaltstart des Servers wird so unsichtbar, und ohne Netz
/// bleibt die App benutzbar.
/// </summary>
public sealed class Stammdatenlader(HttpClient server, IJSRuntime js)
{
    private const string Speicherplatz = "weekplan.stammdaten";

    private static readonly JsonSerializerOptions Format = new(JsonSerializerDefaults.Web);

    private Stammdatensatz? _geladen;

    /// <summary>Im Hintergrund ist ein neuer Stand angekommen.</summary>
    public event Action<Stammdatensatz>? Aufgefrischt;

    public async Task<Stammdatensatz> LadenAsync(CancellationToken ct = default)
    {
        if (_geladen is not null) return _geladen;

        if (await AusDemSpeicherAsync() is { } abgelegt)
        {
            // Absichtlich ohne await: die App startet mit dem gespeicherten
            // Stand, das Nachfragen laeuft daneben. Scheitert es, bleibt der
            // gespeicherte Stand stehen — das ist der Fall im Supermarkt.
            _ = NachfragenAsync(abgelegt.Kennzeichen);
            return _geladen = abgelegt.Satz;
        }

        var frisch = await VomServerAsync(kennzeichen: null, ct)
                     ?? throw new InvalidOperationException(
                         "Der Server meldete unveraendert, obwohl gar kein Kennzeichen mitgeschickt wurde.");

        await InDenSpeicherAsync(frisch);
        return _geladen = frisch.Satz;
    }

    private async Task NachfragenAsync(string? kennzeichen)
    {
        try
        {
            if (await VomServerAsync(kennzeichen, CancellationToken.None) is not { } frisch) return;

            await InDenSpeicherAsync(frisch);
            _geladen = frisch.Satz;
            Aufgefrischt?.Invoke(frisch.Satz);
        }
        catch (Exception)
        {
            // Kein Netz, kalter Server, kaputte Antwort: der gespeicherte Stand
            // gilt weiter. Ein Fehler an dieser Stelle darf die laufende App
            // nicht stoeren — sie zeigt bereits gueltige Daten.
        }
    }

    /// <returns><c>null</c>, wenn der Server „unveraendert" meldet.</returns>
    private async Task<Stand?> VomServerAsync(string? kennzeichen, CancellationToken ct)
    {
        using var anfrage = new HttpRequestMessage(HttpMethod.Get, "stammdaten");
        if (kennzeichen is not null) anfrage.Headers.TryAddWithoutValidation("If-None-Match", kennzeichen);

        HttpResponseMessage antwort;
        try
        {
            antwort = await server.SendAsync(anfrage, ct);
        }
        catch (HttpRequestException fehler)
        {
            throw new StammdatenNichtErreichbarException(fehler);
        }

        using (antwort)
        {
            if (antwort.StatusCode is HttpStatusCode.NotModified) return null;
            antwort.EnsureSuccessStatusCode();

            var satz = await antwort.Content.ReadFromJsonAsync<Stammdatensatz>(Format, ct)
                       ?? throw new InvalidOperationException("Die Stammdaten kamen leer zurueck.");

            return new Stand(antwort.Headers.ETag?.Tag, satz);
        }
    }

    private async Task<Stand?> AusDemSpeicherAsync()
    {
        try
        {
            var roh = await js.InvokeAsync<string?>("localStorage.getItem", Speicherplatz);
            return roh is null ? null : JsonSerializer.Deserialize<Stand>(roh, Format);
        }
        catch (Exception)
        {
            // Ein alter oder halber Eintrag ist kein Grund, die App anzuhalten —
            // dann wird eben frisch geholt.
            return null;
        }
    }

    private async Task InDenSpeicherAsync(Stand stand)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", Speicherplatz, JsonSerializer.Serialize(stand, Format));
        }
        catch (Exception)
        {
            // Voller oder gesperrter Browserspeicher: die App laeuft trotzdem,
            // sie startet beim naechsten Mal nur wieder ohne Vorsprung.
        }
    }

    public sealed record Stand(string? Kennzeichen, Stammdatensatz Satz);
}
