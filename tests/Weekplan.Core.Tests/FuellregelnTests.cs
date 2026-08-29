using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Core.Tagebuch.Contracts;
using Weekplan.Core.Wochenplanung;
using Weekplan.Core.Wochenplanung.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Die Regeln des automatischen Fuellens (Lauf 2026-08-29, Abnahmekriterien
/// 8 bis 10). Sie stehen ausgeschrieben in docs/ernaehrungsplan.md §3: Meal Prep
/// ist die Betriebsart, Fleisch sitzt am Wochenende, und der Refeed-Tag braucht
/// eigene Gerichte.
/// </summary>
public class FuellregelnTests
{
    private static IWochenplanung Planung() =>
        new ServiceCollection().AddWochenplanung().BuildServiceProvider()
            .GetRequiredService<IWochenplanung>();

    private static Rezept R(
        string id, string kategorie, int kcal, int protein,
        bool prep = false, bool wochenende = false, bool refeed = false) =>
        new(id, id, kategorie, 20, false, prep, kcal, protein,
            [new Zutat("X", 10, "Trockenware")], "Kochen.", wochenende, refeed);

    /// <summary>Ein Pool wie der echte: drei Fruehstuecke, vorkochbare Werktagsgerichte,
    /// Wochenendgerichte mit Fleisch, davon zwei refeed-tauglich.</summary>
    private static readonly IReadOnlyList<Rezept> Pool =
    [
        R("f1", "fruehstueck", 700, 65, prep: true),
        R("f2", "fruehstueck", 690, 62, prep: true),
        R("f3", "fruehstueck", 680, 60, prep: true),

        R("m-dal", "mittag", 820, 62, prep: true),
        R("m-chili", "mittag", 810, 60, prep: true),
        R("m-bolo", "mittag", 830, 63, prep: true),
        R("m-burger", "mittag", 950, 55, wochenende: true, refeed: true),
        R("m-pasta", "mittag", 900, 50, wochenende: true, refeed: true),

        R("a-harzer", "abend", 650, 56, prep: true),
        R("a-quark", "abend", 640, 55, prep: true),
        R("a-ofen", "abend", 660, 57, prep: true),
        R("a-haehnchen", "abend", 900, 70, wochenende: true, refeed: true),
        R("a-schnitzel", "abend", 850, 60, wochenende: true)
    ];

    private static readonly Bilanz Ziel = new(
        Grundumsatz: 2055, Alltag: 2630, SportSchnitt: 100, Gesamt: 2730,
        Defizit: 1000, PhasenDefizit: 1000, EigenesTempo: false,
        Normal: 2165, Refeed: 3332, Protein: 160, Wochendefizit: 7000,
        KgProWoche: 0.9, PhasenTempo: 0.9, Sport: new Sportwoche([], 0));

    private static string Gericht(WochenStand w, string tag, string mahlzeit)
        => w.Plan[tag][mahlzeit].Single().RezeptId;

    private static List<string> UeberDieWerktage(WochenStand w, string mahlzeit)
        => [.. Werktage(w).Select(tag => Gericht(w, tag, mahlzeit))];

    private static List<string> Werktage(WochenStand w)
        => [.. new[] { "Mo", "Di", "Mi", "Do", "Fr" }.Where(t => t != w.RefeedTag)];

    /// <summary>Zerlegt eine Folge in ihre Bloecke gleicher Werte: [a,a,a,b,b] → [3,2].</summary>
    private static List<int> Bloecke(IReadOnlyList<string> folge)
    {
        var laengen = new List<int>();
        for (var i = 0; i < folge.Count; i++)
        {
            if (i > 0 && folge[i] == folge[i - 1]) laengen[^1]++;
            else laengen.Add(1);
        }
        return laengen;
    }

    // ── 8a: zwei Sorten in zusammenhaengenden Bloecken ──

    [Theory]
    [InlineData("mittag")]
    [InlineData("abend")]
    public void Werktags_stehen_genau_zwei_Sorten_in_Bloecken(string mahlzeit)
    {
        var woche = Planung().AutomatischFuellen(WochenStand.Leer, Pool, Ziel);

        var folge = UeberDieWerktage(woche, mahlzeit);

        Assert.Equal(2, folge.Distinct().Count());
        Assert.Equal(2, Bloecke(folge).Count);
        Assert.All(Bloecke(folge), laenge => Assert.InRange(laenge, 2, 3));
    }

    // ── 8b: werktags nur Vorkochbares ───────────────────

    [Fact]
    public void Werktags_ist_jedes_Gericht_vorkochbar()
    {
        var woche = Planung().AutomatischFuellen(WochenStand.Leer, Pool, Ziel);
        var nachId = Pool.ToDictionary(r => r.Id);

        foreach (var tag in Werktage(woche))
        {
            Assert.True(nachId[Gericht(woche, tag, "mittag")].Prep, $"{tag} mittag");
            Assert.True(nachId[Gericht(woche, tag, "abend")].Prep, $"{tag} abend");
        }
    }

    // ── 8c: Samstag und Sonntag frisch ──────────────────

    [Fact]
    public void Am_Wochenende_stehen_Wochenendgerichte()
    {
        // Refeed auf Sonntag, damit Samstag ein reiner Wochenendtag ist.
        var leer = WochenStand.Leer with { RefeedTag = "So" };

        var woche = Planung().AutomatischFuellen(leer, Pool, Ziel);
        var nachId = Pool.ToDictionary(r => r.Id);

        Assert.True(nachId[Gericht(woche, "Sa", "mittag")].Wochenende);
        Assert.True(nachId[Gericht(woche, "Sa", "abend")].Wochenende);
    }

    // ── 8d: der Refeed-Tag ──────────────────────────────

    [Fact]
    public void Der_Refeed_Tag_bekommt_refeed_taugliche_Gerichte()
    {
        var woche = Planung().AutomatischFuellen(WochenStand.Leer, Pool, Ziel);
        var nachId = Pool.ToDictionary(r => r.Id);

        Assert.True(nachId[Gericht(woche, woche.RefeedTag, "mittag")].Refeed);
        Assert.True(nachId[Gericht(woche, woche.RefeedTag, "abend")].Refeed);
    }

    /// <summary>
    /// requirements.md §8: liegt der Refeed auf einem Werktag, gewinnt er. Die
    /// Meal-Prep-Bloecke ueberspringen ihn dann — aus fuenf Werktagen werden
    /// vier, also zweimal zwei.
    /// </summary>
    [Fact]
    public void Ein_Refeed_am_Mittwoch_gewinnt_und_die_Bloecke_ueberspringen_ihn()
    {
        var leer = WochenStand.Leer with { RefeedTag = "Mi" };

        var woche = Planung().AutomatischFuellen(leer, Pool, Ziel);
        var nachId = Pool.ToDictionary(r => r.Id);

        Assert.True(nachId["" + Gericht(woche, "Mi", "mittag")].Refeed);

        var folge = UeberDieWerktage(woche, "mittag");   // Mo, Di, Do, Fr
        Assert.Equal(["Mo", "Di", "Do", "Fr"], Werktage(woche));
        Assert.Equal(2, folge.Distinct().Count());
        Assert.Equal([2, 2], Bloecke(folge));
    }

    // ── 8e: das Fruehstueck rotiert taeglich ────────────

    [Fact]
    public void Das_Fruehstueck_wechselt_von_Tag_zu_Tag()
    {
        var woche = Planung().AutomatischFuellen(WochenStand.Leer, Pool, Ziel);

        var folge = Woche.Tage.Select(t => Gericht(woche, t.Kuerzel, "fruehstueck")).ToList();

        for (var i = 1; i < folge.Count; i++) Assert.NotEqual(folge[i - 1], folge[i]);
    }

    // ── 9: nie leer lassen ──────────────────────────────

    /// <summary>
    /// Der Rueckfall traegt auch den Zustand direkt nach dem Ausrollen, wenn die
    /// neuen Merkmale noch an keinem Gericht gepflegt sind.
    /// </summary>
    [Fact]
    public void Ohne_ein_einziges_Wochenendgericht_wird_trotzdem_gefuellt()
    {
        IReadOnlyList<Rezept> ohneMerkmale =
            [.. Pool.Select(r => r with { Wochenende = false, Refeed = false })];

        var woche = Planung().AutomatischFuellen(WochenStand.Leer, ohneMerkmale, Ziel);

        foreach (var tag in Woche.Tage)
        {
            foreach (var mahlzeit in Woche.Mahlzeiten)
            {
                Assert.Single(woche.Plan[tag.Kuerzel][mahlzeit.Schluessel]);
            }
        }
    }

    /// <summary>Gibt es nur eine vorkochbare Sorte, stehen beide Bloecke auf ihr — statt leer.</summary>
    [Fact]
    public void Eine_einzige_vorkochbare_Sorte_traegt_die_ganze_Arbeitswoche()
    {
        IReadOnlyList<Rezept> knapp =
        [
            R("f1", "fruehstueck", 700, 65),
            R("m1", "mittag", 820, 62, prep: true),
            R("m-frisch", "mittag", 900, 50, wochenende: true),
            R("a1", "abend", 650, 56, prep: true),
            R("a-frisch", "abend", 850, 60, wochenende: true)
        ];

        var woche = Planung().AutomatischFuellen(WochenStand.Leer, knapp, Ziel);

        foreach (var tag in Werktage(woche))
        {
            Assert.Equal("m1", Gericht(woche, tag, "mittag"));
            Assert.Equal("a1", Gericht(woche, tag, "abend"));
        }
    }

    // ── 10: nochmal druecken ────────────────────────────

    [Fact]
    public void Zweimal_fuellen_ergibt_eine_andere_aber_ebenso_richtige_Woche()
    {
        var planung = Planung();

        var erste = planung.AutomatischFuellen(WochenStand.Leer, Pool, Ziel);
        var zweite = planung.AutomatischFuellen(erste, Pool, Ziel);

        var vorher = Woche.Tage
            .SelectMany(t => Woche.Mahlzeiten.Select(m => Gericht(erste, t.Kuerzel, m.Schluessel)))
            .ToList();
        var nachher = Woche.Tage
            .SelectMany(t => Woche.Mahlzeiten.Select(m => Gericht(zweite, t.Kuerzel, m.Schluessel)))
            .ToList();

        Assert.NotEqual(vorher, nachher);

        // …und die Regeln halten weiter.
        var folge = UeberDieWerktage(zweite, "mittag");
        Assert.Equal(2, folge.Distinct().Count());
        Assert.All(Bloecke(folge), laenge => Assert.InRange(laenge, 2, 3));
    }

    // ── Was von der alten Bewertung bleibt ──────────────

    /// <summary>
    /// Die Bloecke schlagen die Tagesgenauigkeit — ein Gericht muss zwei bis drei
    /// Tagen gerecht werden. Der Tag darf darum weiter vom Ziel liegen als
    /// frueher, aber nicht beliebig weit.
    /// </summary>
    [Fact]
    public void Jeder_Tag_bleibt_in_Reichweite_seines_Ziels()
    {
        var planung = Planung();
        var woche = planung.AutomatischFuellen(WochenStand.Leer, Pool, Ziel);

        foreach (var tag in Woche.Tage)
        {
            var ziel = tag.Kuerzel == woche.RefeedTag ? Ziel.Refeed : Ziel.Normal;
            var (kcal, _) = planung.Tagessumme(woche, tag.Kuerzel, Pool);

            Assert.InRange(kcal, ziel - 500, ziel + 500);
        }
    }

    // ── Der Rueckfall spricht (Retro 2026-08-29) ────────

    [Fact]
    public void Ein_vollstaendig_gepflegter_Pool_gibt_keine_Hinweise()
    {
        Assert.Empty(Planung().Fuellhinweise(Pool));
    }

    [Fact]
    public void Ein_ungepflegter_Pool_nennt_jede_Regel_die_nicht_greifen_kann()
    {
        IReadOnlyList<Rezept> ohneMerkmale =
            [.. Pool.Select(r => r with { Prep = false, Wochenende = false, Refeed = false })];

        var hinweise = Planung().Fuellhinweise(ohneMerkmale);

        Assert.Equal(3, hinweise.Count);
        Assert.Contains(hinweise, h => h.Contains("vorkochbar"));
        Assert.Contains(hinweise, h => h.Contains("Wochenendgericht"));
        Assert.Contains(hinweise, h => h.Contains("refeed-tauglich"));
    }

    [Fact]
    public void Fehlt_nur_eine_Markierung_steht_auch_nur_ein_Hinweis()
    {
        IReadOnlyList<Rezept> ohneRefeed = [.. Pool.Select(r => r with { Refeed = false })];

        var hinweis = Assert.Single(Planung().Fuellhinweise(ohneRefeed));

        Assert.Contains("refeed-tauglich", hinweis);
    }

    /// <summary>
    /// Ein Gericht, das zugleich vorkochbar und Wochenendgericht ist, stuende
    /// sonst zweimal in derselben Woche: einmal am Wochenende, einmal im Block.
    /// </summary>
    [Fact]
    public void Ein_Gericht_fuer_beides_steht_trotzdem_nur_einmal_in_der_Woche()
    {
        IReadOnlyList<Rezept> doppeldeutig =
        [
            R("f1", "fruehstueck", 700, 65),
            R("m-beides", "mittag", 850, 60, prep: true, wochenende: true, refeed: true),
            R("m-prep-a", "mittag", 820, 62, prep: true),
            R("m-prep-b", "mittag", 830, 63, prep: true),
            R("a-beides", "abend", 700, 58, prep: true, wochenende: true, refeed: true),
            R("a-prep-a", "abend", 650, 56, prep: true),
            R("a-prep-b", "abend", 660, 57, prep: true)
        ];

        var woche = Planung().AutomatischFuellen(WochenStand.Leer, doppeldeutig, Ziel);

        foreach (var mahlzeit in new[] { "mittag", "abend" })
        {
            var tage = Woche.Tage
                .Where(t => Gericht(woche, t.Kuerzel, mahlzeit).EndsWith("beides"))
                .Select(t => t.Kuerzel)
                .ToList();

            // Hoechstens die beiden Einzeltage (Refeed und Wochenende), nie ein Block.
            Assert.All(tage, tag => Assert.Contains(tag, new[] { "Sa", "So" }));
        }
    }
}
