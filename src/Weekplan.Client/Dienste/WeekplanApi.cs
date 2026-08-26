using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Client.Dienste;

/// <summary>Das Merkmal gilt nicht mehr — der Aufrufer muss zur Anmeldung schicken.</summary>
public sealed class NichtAngemeldetException() : Exception("Nicht angemeldet.");

public sealed record AnmeldeAnfrage(string Benutzername, string Passwort);
public sealed record AnmeldeAntwort(string Merkmal);

/// <summary>Alle Rufe an den Server. Das Merkmal reist als Bearer-Kopfzeile mit.</summary>
public sealed class WeekplanApi(HttpClient http, Sitzung sitzung)
{
    /// <returns>Das Merkmal, oder <c>null</c> wenn Name oder Passwort nicht stimmen.</returns>
    public async Task<string?> AnmeldenAsync(string benutzername, string passwort, CancellationToken ct = default)
    {
        var antwort = await http.PostAsJsonAsync("anmeldung", new AnmeldeAnfrage(benutzername, passwort), ct);

        if (antwort.StatusCode is HttpStatusCode.Unauthorized) return null;
        if (antwort.StatusCode is HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("Zu viele Versuche. Warte eine Minute.");
        }
        antwort.EnsureSuccessStatusCode();

        return (await antwort.Content.ReadFromJsonAsync<AnmeldeAntwort>(ct))!.Merkmal;
    }

    public Task<ProfilStand> ProfilAsync(CancellationToken ct = default)
        => HolenAsync<ProfilStand>("tagebuch/profil", ct);

    public Task ProfilSpeichernAsync(ProfilStand profil, CancellationToken ct = default)
        => SchreibenAsync("tagebuch/profil", profil, ct);

    public Task<WochenStand> WocheAsync(CancellationToken ct = default)
        => HolenAsync<WochenStand>("tagebuch/woche", ct);

    public Task WocheSpeichernAsync(WochenStand woche, CancellationToken ct = default)
        => SchreibenAsync("tagebuch/woche", woche, ct);

    private async Task<T> HolenAsync<T>(string pfad, CancellationToken ct)
    {
        using var anfrage = Mit(HttpMethod.Get, pfad);
        using var antwort = await http.SendAsync(anfrage, ct);
        Pruefen(antwort);
        return (await antwort.Content.ReadFromJsonAsync<T>(ct))!;
    }

    private async Task SchreibenAsync<T>(string pfad, T inhalt, CancellationToken ct)
    {
        using var anfrage = Mit(HttpMethod.Put, pfad);
        anfrage.Content = JsonContent.Create(inhalt);
        using var antwort = await http.SendAsync(anfrage, ct);
        Pruefen(antwort);
    }

    private HttpRequestMessage Mit(HttpMethod verfahren, string pfad)
    {
        var anfrage = new HttpRequestMessage(verfahren, pfad);
        if (sitzung.Merkmal is { } merkmal)
        {
            anfrage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", merkmal);
        }
        return anfrage;
    }

    private static void Pruefen(HttpResponseMessage antwort)
    {
        if (antwort.StatusCode is HttpStatusCode.Unauthorized) throw new NichtAngemeldetException();
        antwort.EnsureSuccessStatusCode();
    }
}
