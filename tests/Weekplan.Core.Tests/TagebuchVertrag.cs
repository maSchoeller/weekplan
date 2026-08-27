using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Was jede Ablage koennen muss, unabhaengig davon, worauf sie ablegt. Es gibt
/// zwei Umsetzungen hinter derselben Naht — Dateien lokal, Cosmos in Azure —
/// und sie duerfen sich fuer den Nutzer nicht unterscheiden. Darum stehen die
/// Faelle hier einmal und werden von beiden geerbt.
/// </summary>
public abstract class TagebuchVertrag
{
    /// <summary>Ein frisches Tagebuch auf der zu pruefenden Ablage.</summary>
    protected abstract ITagebuch Tagebuch();

    /// <summary>
    /// Nutzernamen werden je Laufinstanz eindeutig gemacht. Die Dateiablage
    /// bekommt zwar einen frischen Ordner, Cosmos aber nicht — dort liegen die
    /// Dokumente frueherer Laeufe noch, und ein fester Name waere ein Test, der
    /// beim zweiten Mal etwas anderes sieht.
    /// </summary>
    private readonly string _lauf = Guid.NewGuid().ToString("n")[..8];

    protected string Nutzer(string name) => $"{_lauf}-{name}";

    [Fact]
    public async Task Ein_frisches_Konto_hat_leere_Staende()
    {
        var t = Tagebuch();

        Assert.Equal(ProfilStand.Leer, await t.ProfilAsync(Nutzer("marvin")));
        Assert.Equal(WochenStand.Leer.RefeedTag, (await t.WocheAsync(Nutzer("marvin"))).RefeedTag);
    }

    [Fact]
    public async Task Ein_Konto_wird_unter_seinem_Benutzernamen_gefunden()
    {
        var t = Tagebuch();
        await t.KontoAnlegenAsync(new Konto(Nutzer("marvin"), Nutzer("Marvin"), "hash"));

        var gefunden = await t.KontoAsync(Nutzer("Marvin"));

        Assert.NotNull(gefunden);
        Assert.Equal(Nutzer("marvin"), gefunden.NutzerId);
        Assert.Equal("hash", gefunden.PasswortHash);
    }

    [Fact]
    public async Task Der_Benutzername_wird_ohne_Ruecksicht_auf_Gross_und_Klein_gesucht()
    {
        var t = Tagebuch();
        await t.KontoAnlegenAsync(new Konto(Nutzer("marvin"), Nutzer("Marvin"), "hash"));

        Assert.NotNull(await t.KontoAsync(Nutzer("MARVIN")));
        Assert.NotNull(await t.KontoAsync(Nutzer("marvin")));
    }

    [Fact]
    public async Task Ein_unbekanntes_Konto_ist_nichts()
    {
        Assert.Null(await Tagebuch().KontoAsync(Nutzer("niemand")));
    }

    [Fact]
    public async Task Ein_vergebener_Benutzername_wird_nicht_zweimal_angelegt()
    {
        var t = Tagebuch();
        await t.KontoAnlegenAsync(new Konto(Nutzer("marvin"), Nutzer("Marvin"), "hash"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => t.KontoAnlegenAsync(new Konto(Nutzer("marvin"), Nutzer("Marvin"), "anderer")));
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

        await t.ProfilSpeichernAsync(Nutzer("marvin"), profil);

        // Nicht Assert.Equal auf den ganzen Record: Records vergleichen Listen
        // per Referenz, der Vergleich waere also blind fuer den Verlauf.
        var zurueck = await Tagebuch().ProfilAsync(Nutzer("marvin"));
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

        await t.WocheSpeichernAsync(Nutzer("marvin"), woche);

        var zurueck = await Tagebuch().WocheAsync(Nutzer("marvin"));
        Assert.Equal("So", zurueck.RefeedTag);
        Assert.True(zurueck.HakenWoche["Haferflocken"]);
        Assert.Equal(2, zurueck.Plan["Mo"]["mittag"].Single().Portionen);
    }

    [Fact]
    public async Task Ein_ueberschriebener_Stand_ersetzt_den_alten()
    {
        var t = Tagebuch();
        await t.ProfilSpeichernAsync(Nutzer("marvin"), ProfilStand.Leer with { GewichtKg = 84.3 });
        await t.ProfilSpeichernAsync(Nutzer("marvin"), ProfilStand.Leer with { GewichtKg = 83.9 });

        Assert.Equal(83.9, (await Tagebuch().ProfilAsync(Nutzer("marvin"))).GewichtKg);
    }

    [Fact]
    public async Task Zwei_Nutzer_sehen_einander_nicht()
    {
        var t = Tagebuch();
        await t.ProfilSpeichernAsync(Nutzer("marvin"), ProfilStand.Leer with { GewichtKg = 84 });
        await t.ProfilSpeichernAsync(Nutzer("andere"), ProfilStand.Leer with { GewichtKg = 61 });

        Assert.Equal(84, (await t.ProfilAsync(Nutzer("marvin"))).GewichtKg);
        Assert.Equal(61, (await t.ProfilAsync(Nutzer("andere"))).GewichtKg);
    }

    [Fact]
    public async Task Ein_Nutzername_mit_Trennern_wird_abgewiesen()
    {
        var t = Tagebuch();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => t.ProfilSpeichernAsync("../../ausbruch", ProfilStand.Leer));
    }
}
