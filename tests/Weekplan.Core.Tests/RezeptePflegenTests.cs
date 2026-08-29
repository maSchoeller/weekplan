using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Stammdaten;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Rezepte anlegen, aendern und loeschen — der Weg, den Claude Code ueber den
/// MCP-Endpunkt geht. Die Pruefung ist der wichtigste Teil davon: was hier
/// durchrutscht, faellt erst beim Einkaufen oder beim Kochen auf.
///
/// <para>
/// Geprueft gegen die Dateiablage, nicht gegen beide: die Naht selbst deckt
/// <see cref="StammdatenVertrag"/> ab. Hier geht es um Regeln, nicht um
/// Speicherung — und die Kennungen entstehen aus dem Namen, waeren in Cosmos
/// also nicht je Lauf eindeutig.
/// </para>
/// </summary>
public sealed class RezeptePflegenTests : IDisposable
{
    private readonly string _ordner = Path.Combine(
        Path.GetTempPath(), "weekplan-tests", Guid.NewGuid().ToString("n"));

    private readonly IStammdaten _quelle;

    public RezeptePflegenTests()
    {
        _quelle = new ServiceCollection().AddStammdatenInDateien(_ordner).BuildServiceProvider()
            .GetRequiredService<IStammdaten>();

        _quelle.BefuellenAsync(new Stammdatensatz(
            new Rezeptdaten("Hinweis", ["Konserven", "Obst & Gemüse", "Kühlregal"], []),
            new Trainingsdaten("T", new Dictionary<string, MetWert>(), [],
                new Kraftplan("", "", []), []),
            new Grundstockdaten("G", []))).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (Directory.Exists(_ordner)) Directory.Delete(_ordner, recursive: true);
    }

    private static Rezeptentwurf Entwurf(
        string name = "Chili sin Carne",
        string kategorie = "mittag",
        int zeitMin = 40,
        int kcal = 829,
        int protein = 52,
        IReadOnlyList<Zutat>? zutaten = null,
        string anleitung = "## Vorbereitung\nZwiebel würfeln.")
        => new(name, kategorie, zeitMin, Kalt: true, Prep: false, kcal, protein,
               zutaten ?? [new Zutat("Kidneybohnen", 150, "Konserven")], anleitung);

    // ── Anlegen ─────────────────────────────────────────

    [Fact]
    public async Task Ein_neues_Rezept_bekommt_seine_Kennung_aus_dem_Namen()
    {
        var angelegt = await _quelle.AnlegenAsync(Entwurf());

        Assert.Equal("chili-sin-carne", angelegt.Id);
        Assert.Equal("Chili sin Carne", angelegt.Name);
        Assert.NotNull(await _quelle.RezeptAsync("chili-sin-carne"));
    }

    [Theory]
    [InlineData("Möhren-Süßkartoffel-Curry", "moehren-suesskartoffel-curry")]
    [InlineData("Rührei & Brot", "ruehrei-brot")]
    [InlineData("  Linsen   Dal  ", "linsen-dal")]
    [InlineData("Ofen-Feta (mit Kichererbsen)", "ofen-feta-mit-kichererbsen")]
    [InlineData("Bowl 2.0", "bowl-2-0")]
    public async Task Umlaute_und_Sonderzeichen_werden_zu_einer_lesbaren_Kennung(string name, string kennung)
    {
        var angelegt = await _quelle.AnlegenAsync(Entwurf(name: name));

        Assert.Equal(kennung, angelegt.Id);
    }

    /// <summary>Sonst ueberschriebe ein zweites „Chili sin Carne" stillschweigend das erste.</summary>
    [Fact]
    public async Task Ein_zweites_Rezept_gleichen_Namens_wird_abgewiesen()
    {
        await _quelle.AnlegenAsync(Entwurf());

        var fehler = await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AnlegenAsync(Entwurf(anleitung: "Etwas anderes.")));

        Assert.Contains("chili-sin-carne", fehler.Message);
        Assert.Equal("## Vorbereitung\nZwiebel würfeln.", (await _quelle.RezeptAsync("chili-sin-carne"))!.Anleitung);
    }

    [Fact]
    public async Task Ein_Name_ohne_brauchbare_Zeichen_wird_abgewiesen()
        => await Assert.ThrowsAsync<StammdatenUngueltigException>(() => _quelle.AnlegenAsync(Entwurf(name: "!!!")));

    // ── Pruefung ────────────────────────────────────────

    // Akzeptanzkriterium 2: die Absage nennt die erlaubten Abteilungen.
    [Fact]
    public async Task Eine_unbekannte_Abteilung_wird_abgewiesen_und_die_Absage_nennt_die_erlaubten()
    {
        var fehler = await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AnlegenAsync(Entwurf(zutaten: [new Zutat("Tofu", 100, "Tiefkühl")])));

        Assert.Contains("Tiefkühl", fehler.Message);
        Assert.Contains("Konserven", fehler.Message);
        Assert.Contains("Obst & Gemüse", fehler.Message);
        Assert.Contains("Kühlregal", fehler.Message);
    }

    [Fact]
    public async Task Eine_unbekannte_Kategorie_wird_abgewiesen_und_die_Absage_nennt_die_erlaubten()
    {
        var fehler = await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AnlegenAsync(Entwurf(kategorie: "nachtisch")));

        Assert.Contains("nachtisch", fehler.Message);
        foreach (var erlaubt in Kategorien.Erlaubt) Assert.Contains(erlaubt, fehler.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Ein_Rezept_ohne_Namen_wird_abgewiesen(string name)
        => await Assert.ThrowsAsync<StammdatenUngueltigException>(() => _quelle.AnlegenAsync(Entwurf(name: name)));

    [Fact]
    public async Task Ein_Rezept_ohne_Anleitung_wird_abgewiesen()
        => await Assert.ThrowsAsync<StammdatenUngueltigException>(() => _quelle.AnlegenAsync(Entwurf(anleitung: "  ")));

    [Fact]
    public async Task Ein_Rezept_ohne_Zutaten_wird_abgewiesen()
        => await Assert.ThrowsAsync<StammdatenUngueltigException>(() => _quelle.AnlegenAsync(Entwurf(zutaten: [])));

    [Theory]
    [InlineData(0, 52, 40)]
    [InlineData(829, 0, 40)]
    [InlineData(829, 52, 0)]
    [InlineData(-1, 52, 40)]
    public async Task Nullen_und_negative_Zahlen_werden_abgewiesen(int kcal, int protein, int zeit)
        => await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AnlegenAsync(Entwurf(kcal: kcal, protein: protein, zeitMin: zeit)));

    /// <summary>Eine Zutat ohne Menge waere auf der Einkaufsliste ein Posten ueber nichts.</summary>
    [Fact]
    public async Task Eine_Zutat_ohne_Gramm_und_ohne_Stueck_wird_abgewiesen()
        => await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AnlegenAsync(Entwurf(zutaten: [new Zutat("Salz", 0, "Konserven")])));

    [Fact]
    public async Task Eine_Zutat_in_Stueck_ist_in_Ordnung()
    {
        var angelegt = await _quelle.AnlegenAsync(
            Entwurf(zutaten: [new Zutat("Ei", 0, "Kühlregal", Stk: 2)]));

        Assert.Equal(2, angelegt.Zutaten[0].Stk);
    }

    [Fact]
    public async Task Eine_masslos_lange_Anleitung_wird_abgewiesen()
        => await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AnlegenAsync(Entwurf(anleitung: new string('x', 20_001))));

    /// <summary>
    /// Alle Verstoesse auf einmal: wer korrigiert, soll nicht fuenfmal
    /// nacheinander abgewiesen werden.
    /// </summary>
    [Fact]
    public async Task Mehrere_Verstoesse_stehen_alle_in_einer_Absage()
    {
        var fehler = await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AnlegenAsync(Entwurf(
                kategorie: "nachtisch", kcal: 0,
                zutaten: [new Zutat("Tofu", 100, "Tiefkühl")])));

        Assert.Contains("nachtisch", fehler.Message);
        Assert.Contains("Tiefkühl", fehler.Message);
        Assert.Contains("kcal", fehler.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Aendern ─────────────────────────────────────────

    [Fact]
    public async Task Ein_vorhandenes_Rezept_wird_ersetzt_und_behaelt_seine_Kennung()
    {
        await _quelle.AnlegenAsync(Entwurf());

        var geaendert = await _quelle.AendernAsync("chili-sin-carne",
            Entwurf(name: "Chili sin Carne", kcal: 760, anleitung: "## Neu\nAnders."));

        Assert.Equal("chili-sin-carne", geaendert.Id);
        Assert.Equal(760, geaendert.Kcal);
        Assert.Equal("## Neu\nAnders.", (await _quelle.RezeptAsync("chili-sin-carne"))!.Anleitung);
    }

    /// <summary>Aendern legt nicht an — sonst entstuende bei einem Tippfehler ein zweites Rezept.</summary>
    [Fact]
    public async Task Aendern_eines_unbekannten_Rezepts_wird_abgewiesen()
    {
        var fehler = await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AendernAsync("gibt-es-nicht", Entwurf()));

        Assert.Contains("gibt-es-nicht", fehler.Message);
        Assert.Null(await _quelle.RezeptAsync("gibt-es-nicht"));
    }

    [Fact]
    public async Task Auch_beim_Aendern_wird_geprueft()
    {
        await _quelle.AnlegenAsync(Entwurf());

        await Assert.ThrowsAsync<StammdatenUngueltigException>(
            () => _quelle.AendernAsync("chili-sin-carne", Entwurf(kategorie: "nachtisch")));
    }

    // ── Loeschen ────────────────────────────────────────

    [Fact]
    public async Task Ein_geloeschtes_Rezept_ist_weg()
    {
        await _quelle.AnlegenAsync(Entwurf());

        Assert.True(await _quelle.LoeschenAsync("chili-sin-carne"));
        Assert.Null(await _quelle.RezeptAsync("chili-sin-carne"));
        Assert.Empty((await _quelle.AllesAsync()).Rezepte.Rezepte);
    }

    [Fact]
    public async Task Ein_unbekanntes_Rezept_zu_loeschen_meldet_false_und_ist_kein_Fehler()
        => Assert.False(await _quelle.LoeschenAsync("gibt-es-nicht"));
}
