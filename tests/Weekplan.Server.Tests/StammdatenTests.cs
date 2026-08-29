using System.Net;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Server.Tests;

/// <summary>
/// Der Leseweg zu den Stammdaten. Er ist bewusst ohne Anmeldung erreichbar —
/// Rezepte sind kein Geheimnis, und der Client soll sie zeigen koennen, bevor
/// jemand angemeldet ist.
/// </summary>
public sealed class StammdatenTests(ServerFixture server) : IClassFixture<ServerFixture>
{
    // Akzeptanzkriterium 1: die App liest die Rezepte aus der Datenbank.
    [Fact]
    public async Task Stammdaten_sind_ohne_Anmeldung_lesbar()
    {
        await server.StammdatenBefuellenAsync();

        var antwort = await server.CreateClient().GetAsync("/stammdaten");

        Assert.Equal(HttpStatusCode.OK, antwort.StatusCode);
        var satz = await antwort.Content.ReadFromJsonAsync<Stammdatensatz>();
        Assert.Equal("Chili sin Carne", Assert.Single(satz!.Rezepte.Rezepte).Name);
        Assert.Contains("Konserven", satz.Rezepte.Abteilungen);
        Assert.Equal("Anlauf", satz.Training.Phasen[0].Name);
        Assert.Equal("Trockenware", satz.Grundstock.Gruppen[0].Name);
    }

    /// <summary>
    /// Die Anleitung ist Markdown und muss unveraendert durchkommen — Zeilen-
    /// umbrueche und Rautenzeichen sind ihre Struktur, nicht ihr Schmuck.
    /// </summary>
    [Fact]
    public async Task Die_Anleitung_kommt_als_Markdown_unveraendert_durch()
    {
        await server.StammdatenBefuellenAsync();

        var satz = await server.CreateClient().GetFromJsonAsync<Stammdatensatz>("/stammdaten");

        Assert.Equal(ServerFixture.Anleitung, satz!.Rezepte.Rezepte[0].Anleitung);
    }

    // Akzeptanzkriterium 7: der Zwischenspeicher des Clients braucht ein Kennzeichen.
    [Fact]
    public async Task Die_Antwort_traegt_ein_ETag()
    {
        await server.StammdatenBefuellenAsync();

        var antwort = await server.CreateClient().GetAsync("/stammdaten");

        Assert.NotNull(antwort.Headers.ETag);
        Assert.False(antwort.Headers.ETag!.IsWeak);
    }

    [Fact]
    public async Task Mit_demselben_ETag_antwortet_der_Server_ohne_Inhalt()
    {
        await server.StammdatenBefuellenAsync();
        var client = server.CreateClient();

        var erste = await client.GetAsync("/stammdaten");
        var etag = erste.Headers.ETag!.Tag;

        using var anfrage = new HttpRequestMessage(HttpMethod.Get, "/stammdaten");
        anfrage.Headers.Add("If-None-Match", etag);
        var zweite = await client.SendAsync(anfrage);

        Assert.Equal(HttpStatusCode.NotModified, zweite.StatusCode);
        Assert.Empty(await zweite.Content.ReadAsByteArrayAsync());
        Assert.Equal(etag, zweite.Headers.ETag!.Tag);
    }

    /// <summary>
    /// Client und Server liegen auf verschiedenen Herkuenften. Ueber diese
    /// Grenze gibt ein Browser nur wenige Kopfzeilen frei, und <c>ETag</c>
    /// gehoert nicht dazu — ohne diese Freigabe liest der Client sein eigenes
    /// Kennzeichen nicht und laedt bei jeder Pruefung die vollen 49 KB neu.
    /// Im Browser gefunden, nicht im Test: hier steht er jetzt.
    /// </summary>
    [Fact]
    public async Task Das_ETag_ist_ueber_die_Herkunftsgrenze_hinweg_lesbar()
    {
        await server.StammdatenBefuellenAsync();

        using var anfrage = new HttpRequestMessage(HttpMethod.Get, "/stammdaten");
        anfrage.Headers.Add("Origin", ServerFixture.ClientHerkunft);
        var antwort = await server.CreateClient().SendAsync(anfrage);

        Assert.True(antwort.Headers.TryGetValues("Access-Control-Expose-Headers", out var freigegeben),
            "Der Server gibt keine Kopfzeile ueber die Herkunftsgrenze frei.");
        Assert.Contains(freigegeben!, wert => wert.Contains("ETag", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Ein_fremdes_ETag_liefert_die_Daten()
    {
        await server.StammdatenBefuellenAsync();
        var client = server.CreateClient();

        using var anfrage = new HttpRequestMessage(HttpMethod.Get, "/stammdaten");
        anfrage.Headers.Add("If-None-Match", "\"veraltet\"");
        var antwort = await client.SendAsync(anfrage);

        Assert.Equal(HttpStatusCode.OK, antwort.StatusCode);
    }
}
