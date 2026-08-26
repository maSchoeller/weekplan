using System.Net;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Server.Tests;

public sealed class ZugangTests(ServerFixture server) : IClassFixture<ServerFixture>
{
    // Akzeptanzkriterium 5: ohne Anmeldung ist keine Zahl erreichbar.
    [Theory]
    [InlineData("/tagebuch/profil")]
    [InlineData("/tagebuch/woche")]
    public async Task Ohne_Merkmal_gibt_es_keine_Daten(string pfad)
    {
        var antwort = await server.CreateClient().GetAsync(pfad);

        Assert.Equal(HttpStatusCode.Unauthorized, antwort.StatusCode);
        Assert.DoesNotContain("GewichtKg", await antwort.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("voelliger-unsinn")]
    [InlineData("")]
    public async Task Ein_untaugliches_Merkmal_oeffnet_nichts(string merkmal)
    {
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {merkmal}");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/tagebuch/profil")).StatusCode);
    }

    [Fact]
    public async Task Ein_falsches_Passwort_meldet_niemanden_an()
    {
        await server.KontoAnlegenAsync();

        var antwort = await server.CreateClient()
            .PostAsJsonAsync("/anmeldung", new AnmeldeAnfrage(ServerFixture.Benutzer, "falsch"));

        Assert.Equal(HttpStatusCode.Unauthorized, antwort.StatusCode);
    }

    [Fact]
    public async Task Ein_unbekannter_Benutzer_bekommt_dieselbe_Antwort_wie_ein_falsches_Passwort()
    {
        var client = server.CreateClient();

        var unbekannt = await client.PostAsJsonAsync("/anmeldung", new AnmeldeAnfrage("niemand", "egal"));
        var falsch = await client.PostAsJsonAsync("/anmeldung",
            new AnmeldeAnfrage(ServerFixture.Benutzer, "falsch"));

        Assert.Equal(unbekannt.StatusCode, falsch.StatusCode);
    }

    [Fact]
    public async Task Es_gibt_keine_Registrierung()
    {
        var antwort = await server.CreateClient()
            .PostAsJsonAsync("/registrierung", new AnmeldeAnfrage("fremder", "geheim"));

        Assert.Equal(HttpStatusCode.NotFound, antwort.StatusCode);
    }
}

public sealed class TagebuchEndpunktTests(ServerFixture server) : IClassFixture<ServerFixture>
{
    // Akzeptanzkriterium 2 und 3: was ein Geraet schreibt, sieht das naechste.
    [Fact]
    public async Task Ein_gespeichertes_Gewicht_sieht_das_naechste_Geraet()
    {
        var geraetA = await server.AngemeldeterClientAsync();
        var profil = ProfilStand.Leer with
        {
            GewichtKg = 84.3,
            Verlauf = [new(new DateOnly(2026, 8, 26), 84.3)]
        };

        (await geraetA.PutAsJsonAsync("/tagebuch/profil", profil)).EnsureSuccessStatusCode();

        var geraetB = await server.AngemeldeterClientAsync();
        var gelesen = await geraetB.GetFromJsonAsync<ProfilStand>("/tagebuch/profil");

        Assert.Equal(84.3, gelesen!.GewichtKg);
        Assert.Equal(84.3, gelesen.Verlauf.Single().Kg);
    }

    [Fact]
    public async Task Eine_gespeicherte_Woche_sieht_das_naechste_Geraet()
    {
        var geraetA = await server.AngemeldeterClientAsync();
        var woche = WochenStand.Leer with
        {
            RefeedTag = "So",
            HakenWoche = new Dictionary<string, bool> { ["Haferflocken"] = true }
        };

        (await geraetA.PutAsJsonAsync("/tagebuch/woche", woche)).EnsureSuccessStatusCode();

        var gelesen = await (await server.AngemeldeterClientAsync())
            .GetFromJsonAsync<WochenStand>("/tagebuch/woche");

        Assert.Equal("So", gelesen!.RefeedTag);
        Assert.True(gelesen.HakenWoche["Haferflocken"]);
    }

}

/// <summary>
/// Eigene Klasse, damit dieser Test einen eigenen Server mit eigenem Datenordner
/// bekommt — „frisch" ist sonst nur frisch, solange kein anderer Test zuerst lief.
/// </summary>
public sealed class FrischesKontoTests(ServerFixture server) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Ein_frisches_Konto_bekommt_leere_Staende_statt_eines_Fehlers()
    {
        var client = await server.AngemeldeterClientAsync();

        var profil = await client.GetFromJsonAsync<ProfilStand>("/tagebuch/profil");
        var woche = await client.GetFromJsonAsync<WochenStand>("/tagebuch/woche");

        Assert.NotNull(profil);
        Assert.Empty(profil.Verlauf);
        Assert.Equal("Sa", woche!.RefeedTag);
    }
}
