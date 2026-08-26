using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Rechnen;
using Weekplan.Core.Rechnen.Contracts;

namespace Weekplan.Core.Tests;

public class RechnerTests
{
    private static IRechner Rechner() =>
        new ServiceCollection().AddRechnen().BuildServiceProvider()
            .GetRequiredService<IRechner>();

    private static Profil Standard(double? tempo = null) =>
        new(GewichtKg: 80, ZielKg: 75, GroesseCm: 180, Alter: 35, ProteinFaktor: 2.0, TempoKgProWoche: tempo);

    private static readonly Dictionary<string, MetWert> Met = new()
    {
        ["laufband5"] = new("Laufband 5 km/h (Tipptempo)", 3.5),
        ["kraft"] = new("Krafttraining", 3.5)
    };

    private static Phase PhaseMitEinemTag(int defizitZiel = 600) => new(
        "p1", "Phase 1", defizitZiel,
        [new Trainingstag("Mo", "Homeoffice", [new Einheit("laufband5", 90)])]);

    // Mifflin-St Jeor (maennlich): 10 × Gewicht + 6,25 × Groesse − 5 × Alter + 5
    [Theory]
    [InlineData(80, 180, 35, 1755)]      // 800 + 1125 − 175 + 5
    [InlineData(60, 165, 50, 1386.25)]   // 600 + 1031,25 − 250 + 5
    public void Grundumsatz_nach_Mifflin_St_Jeor(double kg, double cm, int alter, double erwartet)
    {
        var profil = Standard() with { GewichtKg = kg, GroesseCm = cm, Alter = alter };

        Assert.Equal(erwartet, Rechner().Grundumsatz(profil), precision: 4);
    }

    [Fact]
    public void Alltagsumsatz_ist_Grundumsatz_mal_1_28()
    {
        Assert.Equal(1755 * 1.28, Rechner().Alltagsumsatz(Standard()), precision: 4);
    }

    // (MET − 1) × 1,05 × kg × Minuten/60. Die −1 zieht den Grundumsatz waehrend
    // der Einheit ab, damit nur der zusaetzliche Verbrauch zaehlt.
    [Theory]
    [InlineData(3.5, 90, 80, 315)]     // so steht es auch in der heutigen App
    [InlineData(9.3, 45, 80, 522.9)]
    public void EinheitKcal_ist_netto(double met, int minuten, double kg, double erwartet)
    {
        Assert.Equal(erwartet, Rechner().EinheitKcal(met, minuten, kg), precision: 4);
    }

    [Fact]
    public void PhaseSport_summiert_die_Tage()
    {
        var sport = Rechner().PhaseSport(PhaseMitEinemTag(), Met, gewichtKg: 80);

        var tag = Assert.Single(sport.ProTag);
        Assert.Equal("Mo", tag.Tag);
        Assert.Equal(315, tag.Kcal);
        Assert.Equal(315, sport.Woche);
    }

    [Fact]
    public void PhaseSport_ohne_Phase_ist_leer()
    {
        var sport = Rechner().PhaseSport(null, Met, gewichtKg: 80);

        Assert.Empty(sport.ProTag);
        Assert.Equal(0, sport.Woche);
    }

    [Fact]
    public void PhaseSport_ueberspringt_unbekannte_Einheiten()
    {
        var phase = new Phase("p1", "Phase 1", 600,
            [new Trainingstag("Mo", "Homeoffice", [new Einheit("gibtsnicht", 90)])]);

        Assert.Equal(0, Rechner().PhaseSport(phase, Met, 80).Woche);
    }

    [Fact]
    public void Bilanz_ohne_eigenes_Tempo_nimmt_das_Defizit_der_Phase()
    {
        var b = Rechner().Bilanz(Standard(), PhaseMitEinemTag(600), Met);

        Assert.False(b.EigenesTempo);
        Assert.Equal(600, b.Defizit);
        Assert.Equal(1755, b.Grundumsatz);
        Assert.Equal(2246, b.Alltag);            // 1755 × 1,28 = 2246,4
        Assert.Equal(45, b.SportSchnitt);        // 315 / 7 = 45
        Assert.Equal(2291, b.Gesamt);            // 2246,4 + 45 = 2291,4
        Assert.Equal(1591, b.Normal);            // 2291,4 − 600 × 7/6 = 1591,4
        Assert.Equal(2291, b.Refeed);
        Assert.Equal(150, b.Protein);            // 2,0 × 75
        Assert.Equal(4200, b.Wochendefizit);
    }

    // Tempo und Defizit sind dieselbe Groesse in zwei Einheiten. Ist ein eigenes
    // Tempo gesetzt, dreht sich die Rechnung um: Defizit = kg/Woche × 7700 / 7.
    [Fact]
    public void Bilanz_mit_eigenem_Tempo_dreht_die_Rechnung_um()
    {
        var b = Rechner().Bilanz(Standard(tempo: 0.7), PhaseMitEinemTag(600), Met);

        Assert.True(b.EigenesTempo);
        Assert.Equal(770, b.Defizit);            // 0,7 × 7700 / 7
        Assert.Equal(600, b.PhasenDefizit);
        Assert.Equal(0.7, b.KgProWoche, precision: 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Bilanz_nimmt_kein_Tempo_das_nicht_positiv_ist(double tempo)
    {
        Assert.False(Rechner().Bilanz(Standard(tempo), PhaseMitEinemTag(600), Met).EigenesTempo);
    }

    [Fact]
    public void Schnitt7_mittelt_die_letzten_sieben_Eintraege()
    {
        var verlauf = Enumerable.Range(0, 10)
            .Select(i => new Gewichtseintrag(new DateOnly(2026, 1, 1).AddDays(i), 80 + i))
            .ToList();

        // Eintraege 3..9 → 83..89 → Mittel 86
        Assert.Equal(86, Rechner().Schnitt7(verlauf, 9)!.Value, precision: 6);
    }

    [Fact]
    public void Schnitt7_am_Anfang_mittelt_nur_was_da_ist()
    {
        List<Gewichtseintrag> verlauf =
            [new(new DateOnly(2026, 1, 1), 80), new(new DateOnly(2026, 1, 2), 82)];

        Assert.Equal(81, Rechner().Schnitt7(verlauf, 1)!.Value, precision: 6);
    }

    [Fact]
    public void Schnitt7_ohne_Eintraege_ist_nichts()
    {
        Assert.Null(Rechner().Schnitt7([], 0));
        Assert.Null(Rechner().Schnitt7([new(new DateOnly(2026, 1, 1), 80)], -1));
    }
}
