using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Weekplan.Core.Anmeldung.Contracts;
using Weekplan.Core.Tagebuch;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Server.Tests;

/// <summary>
/// Faehrt den echten Server hoch, mit einem eigenen Datenordner je Lauf.
/// Kein Attrappen-Server: geprueft wird die Verdrahtung, und die faellt in
/// Attrappen nicht auf.
/// </summary>
public sealed class ServerFixture : WebApplicationFactory<Program>
{
    public const string Schluessel = "test-signaturschluessel-mindestens-32!";
    public const string Benutzer = "marvin";
    public const string Passwort = "k0rrekt-pferd";

    private readonly string _ordner = Path.Combine(
        Path.GetTempPath(), "weekplan-servertests", Guid.NewGuid().ToString("n"));

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Anmeldung:Schluessel"] = Schluessel,
            ["Tagebuch:Ordner"] = _ordner
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
