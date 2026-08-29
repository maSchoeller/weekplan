using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Weekplan.Core.Anmeldung.Contracts;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Core.Tagebuch;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Server.Tests;

/// <summary>
/// Faehrt den echten Server hoch, mit einem eigenen Datenordner je Lauf.
/// Kein Attrappen-Server: geprueft wird die Verdrahtung, und die faellt in
/// Attrappen nicht auf.
/// </summary>
public class ServerFixture : WebApplicationFactory<Program>
{
    public const string Schluessel = "test-signaturschluessel-mindestens-32!";
    /// <summary>Die Herkunft des Clients — Anmeldung und Stammdaten kommen von dort.</summary>
    public const string ClientHerkunft = "http://localhost:5180";

    public const string Benutzer = "marvin";
    public const string Passwort = "k0rrekt-pferd";

    /// <summary>Mit Umbruch und Ueberschrift — genau das, was Markdown ausmacht.</summary>
    public const string Anleitung = """
        ## Vorbereitung
        Zwiebel wuerfeln.

        ## Am Herd
        1. Anbraten.
        """;

    private readonly string _ordner = Path.Combine(
        Path.GetTempPath(), "weekplan-servertests", Guid.NewGuid().ToString("n"));

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Anmeldung:Schluessel"] = Schluessel,
            ["Tagebuch:Ordner"] = _ordner,
            ["Stammdaten:Ordner"] = Path.Combine(_ordner, "stammdaten"),
            ["Cors:Origins:0"] = ClientHerkunft
        }));
        return base.CreateHost(builder);
    }

    /// <summary>Legt das Konto an, wie es das Werkzeug taete, und meldet sich an.</summary>
    public async Task<HttpClient> AngemeldeterClientAsync()
    {
        await KontoAnlegenAsync();
        var client = CreateClient();

        var antwort = await client.PostAsJsonAsync("/anmeldung", new AnmeldeAnfrage(Benutzer, Passwort));
        antwort.EnsureSuccessStatusCode();
        var merkmal = (await antwort.Content.ReadFromJsonAsync<AnmeldeAntwort>())!.Merkmal;

        client.DefaultRequestHeaders.Authorization = new("Bearer", merkmal);
        return client;
    }

    /// <summary>
    /// Befuellt die Stammdaten, wie es das Werkzeug taete. Ohne diesen Schritt
    /// hat der Server nichts auszuliefern — und das ist Absicht, kein Versehen.
    /// </summary>
    public async Task StammdatenBefuellenAsync()
    {
        using var bereich = Services.CreateScope();
        var stammdaten = bereich.ServiceProvider.GetRequiredService<IStammdaten>();

        await stammdaten.BefuellenAsync(new Stammdatensatz(
            new Rezeptdaten("Alle Grammangaben pro Portion.", ["Konserven", "Obst & Gemüse"],
            [
                new Rezept("chili-sin-carne", "Chili sin Carne", "mittag", 40, true, 829, 52,
                    [new Zutat("Kidneybohnen", 150, "Konserven")], Anleitung)
            ]),
            new Trainingsdaten("MET-Hinweis",
                new Dictionary<string, MetWert> { ["gehen"] = new("Gehen", 3.5) },
                [new PhasenAnzeige("p1", "Anlauf", "Woche 1–2", "2 Wochen", 500, "Beschreibung",
                    [new TrainingstagDaten("Mo", "Homeoffice", [new EinheitDaten("gehen", 30)])])],
                new Kraftplan("Kurzhanteln", "Ganzkörper", []),
                []),
            new Grundstockdaten("Vorratshinweis",
                [new Gruppe("Trockenware", [new Artikel("Haferflocken", "1.500 g", "4 Wochen")])])));
    }

    public async Task KontoAnlegenAsync()
    {
        using var bereich = Services.CreateScope();
        var tagebuch = bereich.ServiceProvider.GetRequiredService<ITagebuch>();
        if (await tagebuch.KontoAsync(Benutzer) is not null) return;

        var passwoerter = bereich.ServiceProvider.GetRequiredService<IPasswoerter>();
        await tagebuch.KontoAnlegenAsync(new Konto(
            TagebuchServiceCollectionExtensions.NutzerIdVon(Benutzer),
            Benutzer,
            passwoerter.Hashen(Passwort)));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_ordner)) Directory.Delete(_ordner, recursive: true);
    }
}
