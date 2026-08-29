using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Stammdaten;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Trainingsplan, Grundstock und Abteilungen pflegen — der Weg, den Claude Code
/// seit dem Lauf 2026-08-29 zusaetzlich zu den Rezepten geht.
///
/// <para>
/// Der Schwerpunkt liegt auf dem, was still schiefgehen koennte: ein MET-Wert
/// unter 1 dreht den Sportverbrauch ins Negative und senkt die Zielaufnahme,
/// ohne dass irgendwo eine Fehlermeldung stuende. Genau solche Aenderungen
/// muessen an der Pruefung scheitern, nicht erst beim Nutzer auffallen.
/// </para>
/// </summary>
public sealed class PlanPflegenTests : IDisposable
{
    private readonly string _ordner = Path.Combine(
        Path.GetTempPath(), "weekplan-tests", Guid.NewGuid().ToString("n"));

    private readonly IStammdaten _quelle;

    private static readonly Regel[] Regeln =
    [
        new("Plateau-Regel", "Erst 14 Tage Stillstand sind ein Plateau."),
        new("Waage-Regel", "Taeglich wiegen, nur den 7-Tage-Schnitt bewerten.")
    ];

    public PlanPflegenTests()
    {
        _quelle = new ServiceCollection().AddStammdatenInDateien(_ordner).BuildServiceProvider()
            .GetRequiredService<IStammdaten>();

        _quelle.BefuellenAsync(new Stammdatensatz(
            new Rezeptdaten("Hinweis", ["Obst & Gemüse", "Kühlregal", "Konserven"], []),
            new Trainingsdaten("Trainingshinweis", MetWerte(), [Phase()], Kraft(), Regeln),
            new Grundstockdaten("Grundstockhinweis", []))).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (Directory.Exists(_ordner)) Directory.Delete(_ordner, recursive: true);
    }

    // ── Bausteine ───────────────────────────────────────

    private static Dictionary<string, MetWert> MetWerte(double laufband = 3.5)
        => new()
        {
            ["laufband5"] = new MetWert("Laufband 5 km/h", laufband),
            ["kraft"] = new MetWert("Krafttraining", 3.5)
        };

    private static PhasenAnzeige Phase(string typ = "laufband5", int min = 90, int defizit = 600)
        => new("p1", "Phase 1 — Anlauf", "Woche 1–2", "2 Wochen", defizit, "Bewusst zu wenig.",
               [new TrainingstagDaten("Mo", "Homeoffice", [new EinheitDaten(typ, min)])]);

    private static Kraftplan Kraft()
        => new("Kurzhanteln", "Ganzkoerper zweimal die Woche",
               [new KraftEinheit("a", "Kraft A", [new Uebung("Kniebeuge", "3", "8–10", "Ruecken gerade")])]);

    private static Trainingsentwurf Entwurf(
        IReadOnlyDictionary<string, MetWert>? met = null, PhasenAnzeige? phase = null)
        => new("Trainingshinweis", met ?? MetWerte(), [phase ?? Phase()], Kraft());

    private static Rezeptentwurf Gericht(string name, params string[] abteilungen)
        => new(name, "mittag", 30, Kalt: false, Prep: true, 800, 55,
               [.. abteilungen.Select((a, i) => new Zutat($"Zutat {i + 1}", 100, a))],
               "## Kochen\nAlles in den Topf.");

    // ── Training ────────────────────────────────────────

    [Fact]
    public async Task Der_Trainingsplan_laesst_sich_schreiben()
    {
        var geschrieben = await _quelle.TrainingSchreibenAsync(
            Entwurf(phase: Phase(min: 120, defizit: 800)));

        Assert.Equal(120, geschrieben.Phasen[0].Tage[0].Einheiten[0].Min);
        Assert.Equal(800, geschrieben.Phasen[0].DefizitZiel);

        var gelesen = await _quelle.AllesAsync();
        Assert.Equal(120, gelesen.Training.Phasen[0].Tage[0].Einheiten[0].Min);
    }

    /// <summary>
    /// Die Formel aus <c>docs/plan.md</c> §1 lautet (MET − 1) × 1,05 × kg × min/60.
    /// Unter 1 wird der Verbrauch negativ, der Gesamtumsatz sinkt und die
    /// Zielaufnahme faellt — ohne dass irgendwo etwas danach aussieht.
    /// </summary>
    [Theory]
    [InlineData(0.9)]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Ein_MET_Wert_unter_eins_wird_abgelehnt(double met)
    {
        var fehler = await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.TrainingSchreibenAsync(Entwurf(met: MetWerte(laufband: met))));

        Assert.Contains("laufband5", fehler.Message, StringComparison.Ordinal);
        Assert.Contains("1", fehler.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Eine_Einheit_mit_unbekanntem_MET_Typ_wird_abgelehnt()
    {
        var fehler = await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.TrainingSchreibenAsync(Entwurf(phase: Phase(typ: "schwimmen"))));

        Assert.Contains("schwimmen", fehler.Message, StringComparison.Ordinal);
        Assert.Contains("laufband5", fehler.Message, StringComparison.Ordinal);
    }

    /// <summary>Der Aufrufer soll einmal korrigieren koennen, nicht dreimal.</summary>
    [Fact]
    public async Task Die_Absage_zaehlt_alle_Verstoesse_auf_einmal_auf()
    {
        var fehler = await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.TrainingSchreibenAsync(
                Entwurf(met: MetWerte(laufband: 0.5), phase: Phase(typ: "schwimmen", min: 0))));

        Assert.True(fehler.Klagen.Count >= 3, $"Erwartet mindestens drei Klagen, waren {fehler.Klagen.Count}.");
    }

    [Fact]
    public async Task Ohne_Phasen_wird_abgelehnt()
    {
        var ohne = new Trainingsentwurf("Hinweis", MetWerte(), [], Kraft());

        await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.TrainingSchreibenAsync(ohne));
    }

    /// <summary>
    /// Das Regelwerk bleibt lesend. Der Schutz ist der Typ selbst —
    /// <see cref="Trainingsentwurf"/> hat gar kein Regelfeld —, hier wird
    /// nachgewiesen, dass der Dienst die alten Regeln auch wirklich zuruecklegt.
    /// </summary>
    [Fact]
    public async Task Schreiben_laesst_das_Regelwerk_unveraendert()
    {
        var geschrieben = await _quelle.TrainingSchreibenAsync(Entwurf(phase: Phase(min: 45)));

        Assert.Equal(Regeln.Length, geschrieben.Regeln.Count);
        Assert.Equal("Plateau-Regel", geschrieben.Regeln[0].Titel);

        var gelesen = await _quelle.AllesAsync();
        Assert.Equal("Waage-Regel", gelesen.Training.Regeln[1].Titel);
    }

    // ── Grundstock ──────────────────────────────────────

    [Fact]
    public async Task Der_Grundstock_laesst_sich_ersetzen()
    {
        var neu = new Grundstockdaten("Neuer Hinweis",
            [new Gruppe("Trockenware", [new Artikel("Sojagranulat", "500 g", "8 Portionen")])]);

        await _quelle.GrundstockSchreibenAsync(neu);

        var gelesen = await _quelle.AllesAsync();
        Assert.Equal("Neuer Hinweis", gelesen.Grundstock.Hinweis);
        Assert.Equal("Sojagranulat", gelesen.Grundstock.Gruppen[0].Artikel[0].Name);
    }

    [Fact]
    public async Task Ein_Artikel_ohne_Namen_wird_abgelehnt()
    {
        var kaputt = new Grundstockdaten("Hinweis",
            [new Gruppe("Trockenware", [new Artikel("  ", "500 g", "lange")])]);

        await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.GrundstockSchreibenAsync(kaputt));
    }

    // ── Abteilungen ─────────────────────────────────────

    [Fact]
    public async Task Die_Reihenfolge_der_Abteilungen_laesst_sich_aendern()
    {
        var umzug = await _quelle.AbteilungenSchreibenAsync(
            new Abteilungsentwurf("Hinweis", ["Kühlregal", "Obst & Gemüse", "Konserven"]));

        Assert.Equal(0, umzug.Zutaten);
        Assert.Equal(["Kühlregal", "Obst & Gemüse", "Konserven"], umzug.Abteilungen.Abteilungen);
    }

    [Fact]
    public async Task Doppelte_Abteilungen_werden_abgelehnt()
    {
        await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AbteilungenSchreibenAsync(
                new Abteilungsentwurf("Hinweis", ["Kühlregal", "Kühlregal"])));
    }

    [Fact]
    public async Task Eine_leere_Abteilungsliste_wird_abgelehnt()
    {
        await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AbteilungenSchreibenAsync(new Abteilungsentwurf("Hinweis", [])));
    }

    /// <summary>
    /// Die Anforderung sagt: nichts verschwindet, nichts wird ungueltig. Die
    /// betroffenen Zutaten wandern in die Sammelabteilung, und die steht am
    /// Ende — die Reihenfolge ist der Weg durch den Laden, Unsortiertes kommt
    /// zuletzt.
    /// </summary>
    [Fact]
    public async Task Eine_entfernte_Abteilung_schiebt_ihre_Zutaten_nach_Sonstiges()
    {
        await _quelle.AnlegenAsync(Gericht("Betroffen", "Konserven", "Kühlregal"));
        await _quelle.AnlegenAsync(Gericht("Unbeteiligt", "Kühlregal"));

        var umzug = await _quelle.AbteilungenSchreibenAsync(
            new Abteilungsentwurf("Hinweis", ["Obst & Gemüse", "Kühlregal"]));

        Assert.Equal(1, umzug.Zutaten);
        Assert.Equal(1, umzug.Rezepte);
        Assert.Equal("Sonstiges", umzug.Abteilungen.Abteilungen[^1]);

        var betroffen = await _quelle.RezeptAsync("betroffen");
        Assert.Equal("Sonstiges", betroffen!.Zutaten[0].Abt);
        Assert.Equal("Kühlregal", betroffen.Zutaten[1].Abt);

        var unbeteiligt = await _quelle.RezeptAsync("unbeteiligt");
        Assert.Equal("Kühlregal", unbeteiligt!.Zutaten[0].Abt);
    }

    /// <summary>Ohne betroffene Zutaten hat die Sammelabteilung nichts zu suchen.</summary>
    [Fact]
    public async Task Sonstiges_entsteht_nur_wenn_es_gebraucht_wird()
    {
        await _quelle.AnlegenAsync(Gericht("Nur Kuehlregal", "Kühlregal"));

        var umzug = await _quelle.AbteilungenSchreibenAsync(
            new Abteilungsentwurf("Hinweis", ["Kühlregal", "Konserven"]));

        Assert.DoesNotContain("Sonstiges", umzug.Abteilungen.Abteilungen);
    }
}
