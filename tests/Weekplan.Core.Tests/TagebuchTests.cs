using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Tagebuch;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Gegen echte Dateien, nicht gegen eine Attrappe: die Ablage ist genau der
/// Teil, an dem ein Fehler weh taete, und ein Attrappentest wuerde ihn nicht sehen.
/// </summary>
public sealed class TagebuchTests : IDisposable
{
    private readonly string _ordner = Path.Combine(
        Path.GetTempPath(), "weekplan-tests", Guid.NewGuid().ToString("n"));

    private ITagebuch Tagebuch() =>
        new ServiceCollection().AddTagebuchInDateien(_ordner).BuildServiceProvider()
            .GetRequiredService<ITagebuch>();

    public void Dispose()
    {
        if (Directory.Exists(_ordner)) Directory.Delete(_ordner, recursive: true);
    }

    [Fact]
    public async Task Ein_frisches_Konto_hat_leere_Staende()
    {
        var t = Tagebuch();

        Assert.Equal(ProfilStand.Leer, await t.ProfilAsync("marvin"));
        Assert.Equal(WochenStand.Leer.RefeedTag, (await t.WocheAsync("marvin")).RefeedTag);
    }

    [Fact]
    public async Task Ein_Konto_wird_unter_seinem_Benutzernamen_gefunden()
    {
        var t = Tagebuch();
        await t.KontoAnlegenAsync(new Konto("marvin", "Marvin", "hash"));

        var gefunden = await t.KontoAsync("Marvin");

        Assert.NotNull(gefunden);
        Assert.Equal("marvin", gefunden.NutzerId);
        Assert.Equal("hash", gefunden.PasswortHash);
    }

    [Fact]
    public async Task Der_Benutzername_wird_ohne_Ruecksicht_auf_Gross_und_Klein_gesucht()
    {
        var t = Tagebuch();
        await t.KontoAnlegenAsync(new Konto("marvin", "Marvin", "hash"));

        Assert.NotNull(await t.KontoAsync("MARVIN"));
        Assert.NotNull(await t.KontoAsync("marvin"));
    }

    [Fact]
    public async Task Ein_unbekanntes_Konto_ist_nichts()
    {
        Assert.Null(await Tagebuch().KontoAsync("niemand"));
    }

    [Fact]
    public async Task Ein_vergebener_Benutzername_wird_nicht_zweimal_angelegt()
    {
        var t = Tagebuch();
        await t.KontoAnlegenAsync(new Konto("marvin", "Marvin", "hash"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => t.KontoAnlegenAsync(new Konto("marvin", "Marvin", "anderer")));
    }

    [Fact]
    public async Task Ein_gespeichertes_Profil_kommt_unveraendert_zurueck()
    {
        var t = Tagebuch();
        var profil = ProfilStand.Leer with
        {
            GewichtKg = 84.3,
            Zieltermin = new DateOnly(2026, 12, 24),
            TempoKgProWoche = 0.7,
            Verlauf = [new Gewichtseintrag(new DateOnly(2026, 8, 26), 84.3)]
        };

        await t.ProfilSpeichernAsync("marvin", profil);

        // Nicht Assert.Equal auf den ganzen Record: Records vergleichen Listen
        // per Referenz, der Vergleich waere also blind fuer den Verlauf.
        var zurueck = await Tagebuch().ProfilAsync("marvin");
        Assert.Equal(profil with { Verlauf = [] }, zurueck with { Verlauf = [] });
        Assert.Equal(profil.Verlauf, zurueck.Verlauf);
    }

    [Fact]
    public async Task Eine_gespeicherte_Woche_kommt_unveraendert_zurueck()
    {
        var t = Tagebuch();
        var woche = WochenStand.Leer with
        {
            Plan = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>
            {
                ["Mo"] = new Dictionary<string, IReadOnlyList<PlanEintrag>>
                {
                    ["mittag"] = [new PlanEintrag("linsen", 2)]
                }
            },
            RefeedTag = "So",
            HakenWoche = new Dictionary<string, bool> { ["Haferflocken"] = true }
        };

        await t.WocheSpeichernAsync("marvin", woche);

        var zurueck = await Tagebuch().WocheAsync("marvin");
        Assert.Equal("So", zurueck.RefeedTag);
        Assert.True(zurueck.HakenWoche["Haferflocken"]);
        Assert.Equal(2, zurueck.Plan["Mo"]["mittag"].Single().Portionen);
    }

    [Fact]
    public async Task Zwei_Nutzer_sehen_einander_nicht()
    {
        var t = Tagebuch();
        await t.ProfilSpeichernAsync("marvin", ProfilStand.Leer with { GewichtKg = 84 });
        await t.ProfilSpeichernAsync("andere", ProfilStand.Leer with { GewichtKg = 61 });

        Assert.Equal(84, (await t.ProfilAsync("marvin")).GewichtKg);
        Assert.Equal(61, (await t.ProfilAsync("andere")).GewichtKg);
    }

    [Fact]
    public async Task Ein_Nutzername_mit_Pfadtrennern_bricht_nicht_aus_dem_Ordner_aus()
    {
        var t = Tagebuch();

        await Assert.ThrowsAsync<ArgumentException>(
            () => t.ProfilSpeichernAsync("../../ausbruch", ProfilStand.Leer));
    }
}
