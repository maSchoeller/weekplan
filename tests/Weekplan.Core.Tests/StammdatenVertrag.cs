using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Was jede Stammdaten-Ablage koennen muss, unabhaengig davon, worauf sie
/// ablegt. Wie beim Tagebuch gibt es zwei Umsetzungen hinter derselben Naht —
/// Dateien lokal, Cosmos in Azure — und sie duerfen sich nicht unterscheiden.
/// </summary>
public abstract class StammdatenVertrag
{
    /// <summary>Eine frische Quelle auf der zu pruefenden Ablage.</summary>
    protected abstract IStammdaten Quelle();

    /// <summary>
    /// Rezeptkennungen werden je Laufinstanz eindeutig gemacht — die Cosmos-
    /// Ablage behaelt die Dokumente frueherer Laeufe, und ein fester Name waere
    /// ein Test, der beim zweiten Mal etwas anderes sieht.
    /// </summary>
    private readonly string _lauf = Guid.NewGuid().ToString("n")[..8];

    protected string Kennung(string name) => $"{_lauf}-{name}";

    private static readonly string[] Abteilungen = ["Konserven", "Obst & Gemüse", "Trockenware"];

    private Rezept Chili(string anleitung = "Kochen.") => new(
        Kennung("chili"), "Chili sin Carne", "mittag", 40, true, true, 829, 52,
        [
            new Zutat("Kidneybohnen", 150, "Konserven"),
            new Zutat("Zwiebel", 70, "Obst & Gemüse"),
            new Zutat("Olivenöl", 6, "Trockenware", Vorrat: true),
            new Zutat("Ei", 0, "Konserven", Stk: 2)
        ],
        anleitung);

    private Rezept Oats() => new(
        Kennung("oats"), "Overnight Oats", "fruehstueck", 5, true, true, 512, 38,
        [new Zutat("Haferflocken", 80, "Trockenware")],
        "## Abends\nAlles verrühren.");

    private static Trainingsdaten Training() => new(
        "MET-Hinweis",
        new Dictionary<string, MetWert> { ["gehen"] = new("Gehen", 3.5) },
        [new PhasenAnzeige("p1", "Anlauf", "Woche 1–2", "2 Wochen", 500, "Beschreibung",
            [new TrainingstagDaten("Mo", "Homeoffice", [new EinheitDaten("gehen", 30)])])],
        new Kraftplan("Kurzhanteln", "Ganzkörper",
            [new KraftEinheit("k1", "Einheit A", [new Uebung("Kniebeuge", "3", "10", "tief")])]),
        [new Regel("Regel", "Text")]);

    private static Grundstockdaten Grundstock() => new(
        "Vorratshinweis",
        [new Gruppe("Trockenware", [new Artikel("Haferflocken", "1.500 g", "4 Wochen")])]);

    private Stammdatensatz Satz(params Rezept[] rezepte)
        => new(new Rezeptdaten("Alle Grammangaben pro Portion.", Abteilungen, rezepte),
               Training(), Grundstock());

    private async Task<IReadOnlyList<Rezept>> MeineAsync(IStammdaten quelle)
    {
        var alles = await quelle.AllesAsync();
        return [.. alles.Rezepte.Rezepte.Where(r => r.Id.StartsWith(_lauf, StringComparison.Ordinal))];
    }

    [Fact]
    public async Task Ein_geschriebenes_Rezept_kommt_unveraendert_zurueck()
    {
        await Quelle().BefuellenAsync(Satz(Chili()));

        var rezept = Assert.Single(await MeineAsync(Quelle()));

        Assert.Equal(Kennung("chili"), rezept.Id);
        Assert.Equal("Chili sin Carne", rezept.Name);
        Assert.Equal("mittag", rezept.Kategorie);
        Assert.Equal(40, rezept.ZeitMin);
        Assert.True(rezept.Kalt);
        Assert.Equal(829, rezept.Kcal);
        Assert.Equal(52, rezept.Protein);
        Assert.Equal("Kochen.", rezept.Anleitung);
    }

    [Fact]
    public async Task Zutaten_behalten_Reihenfolge_Menge_Abteilung_und_Kennzeichen()
    {
        await Quelle().BefuellenAsync(Satz(Chili()));

        var zutaten = (await MeineAsync(Quelle()))[0].Zutaten;

        Assert.Equal(4, zutaten.Count);
        Assert.Equal(["Kidneybohnen", "Zwiebel", "Olivenöl", "Ei"], zutaten.Select(z => z.Name));
        Assert.Equal(150, zutaten[0].G);
        Assert.Equal("Obst & Gemüse", zutaten[1].Abt);
        Assert.True(zutaten[2].Vorrat);
        Assert.False(zutaten[0].Vorrat);
        Assert.Equal(2, zutaten[3].Stk);
    }

    [Fact]
    public async Task Ein_einzelnes_Rezept_wird_ueber_seine_Kennung_gefunden()
    {
        await Quelle().BefuellenAsync(Satz(Chili(), Oats()));

        var rezept = await Quelle().RezeptAsync(Kennung("oats"));

        Assert.NotNull(rezept);
        Assert.Equal("Overnight Oats", rezept.Name);
        Assert.Equal("## Abends\nAlles verrühren.", rezept.Anleitung);
    }

    [Fact]
    public async Task Eine_unbekannte_Kennung_liefert_null_und_ist_kein_Fehler()
    {
        await Quelle().BefuellenAsync(Satz(Chili()));

        Assert.Null(await Quelle().RezeptAsync(Kennung("gibt-es-nicht")));
    }

    [Fact]
    public async Task Erneutes_Befuellen_ersetzt_ein_gleichnamiges_Rezept()
    {
        await Quelle().BefuellenAsync(Satz(Chili()));
        await Quelle().BefuellenAsync(Satz(Chili("## Neu\nAnders kochen.")));

        var rezept = Assert.Single(await MeineAsync(Quelle()));
        Assert.Equal("## Neu\nAnders kochen.", rezept.Anleitung);
    }

    [Fact]
    public async Task Rezepte_kommen_nach_Namen_sortiert_zurueck()
    {
        await Quelle().BefuellenAsync(Satz(Oats(), Chili()));

        var namen = (await MeineAsync(Quelle())).Select(r => r.Name);

        Assert.Equal(["Chili sin Carne", "Overnight Oats"], namen);
    }

    [Fact]
    public async Task Hinweis_und_Abteilungen_kommen_in_ihrer_Reihenfolge_zurueck()
    {
        await Quelle().BefuellenAsync(Satz(Chili()));

        var rezepte = (await Quelle().AllesAsync()).Rezepte;

        Assert.Equal("Alle Grammangaben pro Portion.", rezepte.Hinweis);
        Assert.Equal(Abteilungen, rezepte.Abteilungen);
    }

    [Fact]
    public async Task Training_kommt_vollstaendig_zurueck()
    {
        await Quelle().BefuellenAsync(Satz(Chili()));

        var training = (await Quelle().AllesAsync()).Training;

        Assert.Equal("MET-Hinweis", training.Hinweis);
        Assert.Equal(3.5, training.MetWerte["gehen"].Met);
        Assert.Equal("Anlauf", training.Phasen[0].Name);
        Assert.Equal(30, training.Phasen[0].Tage[0].Einheiten[0].Min);
        Assert.Equal("Kniebeuge", training.Kraftplan.Einheiten[0].Uebungen[0].Name);
        Assert.Equal("Regel", training.Regeln[0].Titel);
    }

    [Fact]
    public async Task Grundstock_kommt_vollstaendig_zurueck()
    {
        await Quelle().BefuellenAsync(Satz(Chili()));

        var grundstock = (await Quelle().AllesAsync()).Grundstock;

        Assert.Equal("Vorratshinweis", grundstock.Hinweis);
        Assert.Equal("Haferflocken", grundstock.Gruppen[0].Artikel[0].Name);
        Assert.Equal("4 Wochen", grundstock.Gruppen[0].Artikel[0].Reichweite);
    }

}
