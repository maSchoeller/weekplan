using System.Net;

namespace Weekplan.Server.Tests;

/// <summary>
/// <c>/mcp</c> ist der einzige Weg, von aussen zu schreiben. Was hier nicht
/// zuhaelt, kann jeder benutzen, der die Adresse kennt — die Faelle stehen
/// deshalb vor allem anderen.
/// </summary>
public sealed class McpZugangTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private static HttpRequestMessage Ruf(string? schluessel)
    {
        var anfrage = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""",
                System.Text.Encoding.UTF8, "application/json")
        };
        anfrage.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (schluessel is not null) anfrage.Headers.Add("Authorization", $"Bearer {schluessel}");
        return anfrage;
    }

    [Fact]
    public async Task Ohne_Schluessel_kommt_niemand_hinein()
    {
        var antwort = await server.CreateClient().SendAsync(Ruf(null));

        Assert.Equal(HttpStatusCode.Unauthorized, antwort.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("falsch")]
    [InlineData("test-mcp-schluessel-fuer-die-testsx")]
    public async Task Ein_falscher_Schluessel_oeffnet_nichts(string schluessel)
    {
        var antwort = await server.CreateClient().SendAsync(Ruf(schluessel));

        Assert.Equal(HttpStatusCode.Unauthorized, antwort.StatusCode);
    }

    /// <summary>
    /// Der Schluessel gehoert nicht zum Anmelde-Merkmal der App: ein Nutzer-Token
    /// darf keine Rezepte schreiben duerfen, und umgekehrt.
    /// </summary>
    [Fact]
    public async Task Das_Anmelde_Merkmal_der_App_oeffnet_den_Pflegeweg_nicht()
    {
        var angemeldet = await server.AngemeldeterClientAsync();
        var merkmal = angemeldet.DefaultRequestHeaders.Authorization!.Parameter;

        var antwort = await server.CreateClient().SendAsync(Ruf(merkmal));

        Assert.Equal(HttpStatusCode.Unauthorized, antwort.StatusCode);
    }

    [Fact]
    public async Task Mit_dem_richtigen_Schluessel_stehen_die_Werkzeuge_bereit()
    {
        var antwort = await server.CreateClient().SendAsync(Ruf(McpServerFixture.McpSchluessel));

        antwort.EnsureSuccessStatusCode();
        var text = await antwort.Content.ReadAsStringAsync();

        foreach (var werkzeug in new[]
                 {
                     "rezepte_auflisten", "rezept_lesen", "rezept_anlegen",
                     "rezept_aendern", "rezept_loeschen",
                     "abteilungen_lesen", "grundstock_lesen", "training_lesen"
                 })
        {
            Assert.Contains(werkzeug, text);
        }
    }
}

/// <summary>
/// Ohne Schluessel in der Konfiguration entsteht der Endpunkt gar nicht. Das ist
/// die Lage im Standard-<see cref="ServerFixture"/> — und sie wird hier
/// geprueft, weil sie lokal und in den uebrigen Tests gilt.
/// </summary>
public sealed class McpAusTests(ServerFixture server) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Ohne_konfigurierten_Schluessel_gibt_es_den_Endpunkt_nicht()
    {
        var antwort = await server.CreateClient().PostAsync("/mcp",
            new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""",
                System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, antwort.StatusCode);
    }
}
