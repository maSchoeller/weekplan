using System.Text;
using System.Text.Json;

namespace Weekplan.Server.Tests;

/// <summary>
/// Die Werkzeuge ueber das echte Protokoll gerufen — nicht die C#-Methoden
/// direkt. Nur so faellt auf, wenn ein Werkzeug zwar richtig rechnet, seine
/// Eingabe aber gar nicht ankommt.
/// </summary>
public sealed class McpWerkzeugeTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private async Task<JsonElement> RufenAsync(string werkzeug, object argumente)
    {
        var client = server.CreateClient();

        var anfrage = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new { name = werkzeug, arguments = argumente }
            }), Encoding.UTF8, "application/json")
        };
        anfrage.Headers.Accept.ParseAdd("application/json, text/event-stream");
        anfrage.Headers.Add("Authorization", $"Bearer {McpServerFixture.McpSchluessel}");

        var antwort = await client.SendAsync(anfrage);
        antwort.EnsureSuccessStatusCode();

        // Die Streamable-HTTP-Antwort kann als Ereignisstrom kommen; die
        // Nutzlast ist dann die Zeile hinter „data: ".
        var roh = await antwort.Content.ReadAsStringAsync();
        var json = roh.Contains("data:", StringComparison.Ordinal)
            ? roh.Split("data:", StringSplitOptions.None)[^1].Trim()
            : roh;

        return JsonDocument.Parse(json).RootElement;
    }

    private static string TextVon(JsonElement antwort)
        => antwort.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;

    private static bool IstFehler(JsonElement antwort)
        => antwort.GetProperty("result").TryGetProperty("isError", out var f) && f.GetBoolean();

    private static object Entwurf(string name = "Testgericht", string abteilung = "Konserven",
                                  string kategorie = "mittag")
        => new
        {
            rezept = new
            {
                name,
                kategorie,
                zeitMin = 25,
                kalt = false,
                kcal = 700,
                protein = 45,
                zutaten = new[] { new { name = "Kichererbsen", g = 120.0, abt = abteilung, vorrat = false, stk = 0.0 } },
                anleitung = "## Vorbereitung\nAlles bereitstellen."
            }
        };

    [Fact]
    public async Task Ein_Rezept_laesst_sich_anlegen_und_wieder_lesen()
    {
        await server.StammdatenBefuellenAsync();

        var angelegt = await RufenAsync("rezept_anlegen", Entwurf(name: "Kichererbsen Curry"));
        Assert.False(IstFehler(angelegt));
        Assert.Contains("kichererbsen-curry", TextVon(angelegt));

        var gelesen = await RufenAsync("rezept_lesen", new { id = "kichererbsen-curry" });
        Assert.Contains("Alles bereitstellen.", TextVon(gelesen));
    }

    // Akzeptanzkriterium 2, auf dem Weg, den Claude Code wirklich geht.
    [Fact]
    public async Task Eine_unbekannte_Abteilung_wird_abgelehnt_und_die_Absage_nennt_die_erlaubten()
    {
        await server.StammdatenBefuellenAsync();

        var antwort = await RufenAsync("rezept_anlegen",
            Entwurf(name: "Falsche Abteilung", abteilung: "Tiefkühl"));

        Assert.True(IstFehler(antwort));
        var meldung = TextVon(antwort);
        Assert.Contains("Tiefkühl", meldung);
        Assert.Contains("Konserven", meldung);
    }

    [Fact]
    public async Task Ein_zweites_Rezept_gleichen_Namens_wird_abgelehnt()
    {
        await server.StammdatenBefuellenAsync();

        await RufenAsync("rezept_anlegen", Entwurf(name: "Doppelt Gemoppelt"));
        var zweites = await RufenAsync("rezept_anlegen", Entwurf(name: "Doppelt Gemoppelt"));

        Assert.True(IstFehler(zweites));
        Assert.Contains("doppelt-gemoppelt", TextVon(zweites));
    }

    [Fact]
    public async Task Ein_Rezept_laesst_sich_loeschen()
    {
        await server.StammdatenBefuellenAsync();
        await RufenAsync("rezept_anlegen", Entwurf(name: "Weg Damit"));

        var geloescht = await RufenAsync("rezept_loeschen", new { id = "weg-damit" });
        Assert.False(IstFehler(geloescht));

        var gelesen = await RufenAsync("rezept_lesen", new { id = "weg-damit" });
        Assert.True(IstFehler(gelesen));
    }

    /// <summary>
    /// Der Server haelt die Antwort auf <c>/stammdaten</c> im Speicher, damit
    /// sie ein ETag tragen kann. Ein neu angelegtes Rezept muss diesen Stand
    /// verwerfen — sonst sieht die App es erst nach einem Neustart, und niemand
    /// versteht, warum.
    /// </summary>
    [Fact]
    public async Task Ein_neues_Rezept_ist_sofort_ueber_stammdaten_sichtbar()
    {
        await server.StammdatenBefuellenAsync();
        var client = server.CreateClient();

        var vorher = await client.GetAsync("/stammdaten");
        var altesKennzeichen = vorher.Headers.ETag!.Tag;

        await RufenAsync("rezept_anlegen", Entwurf(name: "Frisch Dazu"));

        var nachher = await client.GetAsync("/stammdaten");
        Assert.Contains("frisch-dazu", await nachher.Content.ReadAsStringAsync());
        Assert.NotEqual(altesKennzeichen, nachher.Headers.ETag!.Tag);
    }

    [Fact]
    public async Task Ein_geloeschtes_Rezept_verschwindet_sofort_aus_stammdaten()
    {
        await server.StammdatenBefuellenAsync();
        var client = server.CreateClient();

        await RufenAsync("rezept_anlegen", Entwurf(name: "Kurz Da"));
        Assert.Contains("kurz-da", await client.GetStringAsync("/stammdaten"));

        await RufenAsync("rezept_loeschen", new { id = "kurz-da" });

        Assert.DoesNotContain("kurz-da", await client.GetStringAsync("/stammdaten"));
    }

    [Fact]
    public async Task Abteilungen_und_Grundstock_sind_lesbar()
    {
        await server.StammdatenBefuellenAsync();

        Assert.Contains("Konserven", TextVon(await RufenAsync("abteilungen_lesen", new { })));
        Assert.Contains("Haferflocken", TextVon(await RufenAsync("grundstock_lesen", new { })));
        Assert.Contains("Anlauf", TextVon(await RufenAsync("training_lesen", new { })));
    }
}
