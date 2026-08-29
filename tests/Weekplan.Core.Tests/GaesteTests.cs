using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Tagebuch.Contracts;
using Weekplan.Core.Wochenplanung;
using Weekplan.Core.Wochenplanung.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Zusaetzliche Esser (Lauf 2026-08-29). Der ganze Sinn des Merkmals ist eine
/// Trennung: die Gaestezahl bewegt Einkauf und Kochmenge und darf die eigene
/// Bilanz um kein einziges kcal verschieben.
/// </summary>
public class GaesteTests
{
    private static IWochenplanung Planung() =>
        new ServiceCollection().AddWochenplanung().BuildServiceProvider()
            .GetRequiredService<IWochenplanung>();

    private static readonly string[] Abteilungen = ["Konserven"];

    private static readonly Rezept Chili = new(
        "chili", "Chili sin Carne", "mittag", 40, true, true, 800, 50,
        [new Zutat("Kidneybohnen", 150, "Konserven")], "Kochen.");

    private static readonly Rezept Oats = new(
        "oats", "Overnight Oats", "fruehstueck", 5, true, true, 500, 30,
        [new Zutat("Haferflocken", 80, "Konserven")], "Ruehren.");

    private static WochenStand MitPlan(
        params (string Tag, string Mahlzeit, string RezeptId, int Portionen)[] eintraege)
    {
        var plan = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>();
        foreach (var (tag, mahlzeit, id, portionen) in eintraege)
        {
            var mahlzeiten = plan.TryGetValue(tag, out var da)
                ? new Dictionary<string, IReadOnlyList<PlanEintrag>>(da.ToDictionary(x => x.Key, x => x.Value))
                : [];
            mahlzeiten[mahlzeit] = [new PlanEintrag(id, portionen)];
            plan[tag] = mahlzeiten;
        }
        return WochenStand.Leer with { Plan = plan };
    }

    // ── Die Nachschlagetabelle ──────────────────────────

    [Fact]
    public void Ohne_Angabe_isst_niemand_mit()
    {
        Assert.Equal(0, WochenStand.Leer.Gaeste("Mo", "mittag"));
    }

    // Abnahmekriterium 1: die Zahl am Tag gilt fuer alle drei Mahlzeiten.
    [Fact]
    public void Die_Zahl_am_Tag_gilt_fuer_alle_Mahlzeiten()
    {
        var woche = WochenStand.Leer with { GaesteTag = new Dictionary<string, int> { ["Sa"] = 2 } };

        Assert.Equal(2, woche.Gaeste("Sa", "fruehstueck"));
        Assert.Equal(2, woche.Gaeste("Sa", "mittag"));
        Assert.Equal(2, woche.Gaeste("Sa", "abend"));
        Assert.Equal(0, woche.Gaeste("So", "mittag"));
    }

    /// <summary>
    /// Der Fall aus dem Gespraech: „am Freitag fruehstuecken die Gaeste nicht
    /// mit". Eine gesetzte 0 an der Mahlzeit ist etwas anderes als keine
    /// Angabe — deshalb liegen Tag und Ausnahme in getrennten Sammlungen.
    /// </summary>
    [Fact]
    public void Eine_Mahlzeit_darf_vom_Tag_abweichen_auch_auf_null()
    {
        var woche = WochenStand.Leer with
        {
            GaesteTag = new Dictionary<string, int> { ["Fr"] = 2 },
            GaesteMahlzeit = new Dictionary<string, int> { ["Fr|fruehstueck"] = 0 }
        };

        Assert.Equal(0, woche.Gaeste("Fr", "fruehstueck"));
        Assert.Equal(2, woche.Gaeste("Fr", "mittag"));
    }

    [Fact]
    public void Eine_Ausnahme_wirkt_auch_ohne_Zahl_am_Tag()
    {
        var woche = WochenStand.Leer with
        {
            GaesteMahlzeit = new Dictionary<string, int> { ["Mi|abend"] = 1 }
        };

        Assert.Equal(1, woche.Gaeste("Mi", "abend"));
        Assert.Equal(0, woche.Gaeste("Mi", "mittag"));
    }

    // ── Was die Gaeste bewegen ──────────────────────────

    // Abnahmekriterium 4: die Einkaufsliste rechnet die Gaesteportionen mit.
    [Fact]
    public void Die_Einkaufsliste_rechnet_Gaeste_mit()
    {
        var woche = MitPlan(("Mo", "mittag", "chili", 1)) with
        {
            GaesteTag = new Dictionary<string, int> { ["Mo"] = 2 }
        };

        var liste = Planung().Einkaufsliste(woche, [Chili], Abteilungen);

        // eine eigene Portion plus zwei Gaeste = dreifache Menge
        Assert.Equal(450, liste.Posten.Single(p => p.Name == "Kidneybohnen").Gramm);
    }

    [Fact]
    public void Die_Einkaufsliste_nennt_die_Zahl_der_Gaesteportionen()
    {
        var woche = MitPlan(("Mo", "mittag", "chili", 1), ("Mo", "fruehstueck", "oats", 1)) with
        {
            GaesteTag = new Dictionary<string, int> { ["Mo"] = 2 },
            GaesteMahlzeit = new Dictionary<string, int> { ["Mo|fruehstueck"] = 0 }
        };

        var liste = Planung().Einkaufsliste(woche, [Chili, Oats], Abteilungen);

        Assert.Equal(2, liste.Gaesteportionen);
    }

    /// <summary>
    /// Stehen zwei Gerichte auf einer Mahlzeit, essen die Gaeste beides — sie
    /// essen, was der Nutzer isst. Entschieden in requirements.md §4.1.
    /// </summary>
    [Fact]
    public void Gaeste_wirken_auf_jedes_Gericht_der_Mahlzeit()
    {
        var plan = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>
        {
            ["Mo"] = new Dictionary<string, IReadOnlyList<PlanEintrag>>
            {
                ["mittag"] = [new PlanEintrag("chili", 1), new PlanEintrag("oats", 1)]
            }
        };
        var woche = WochenStand.Leer with
        {
            Plan = plan,
            GaesteTag = new Dictionary<string, int> { ["Mo"] = 1 }
        };

        var liste = Planung().Einkaufsliste(woche, [Chili, Oats], Abteilungen);

        Assert.Equal(300, liste.Posten.Single(p => p.Name == "Kidneybohnen").Gramm);
        Assert.Equal(160, liste.Posten.Single(p => p.Name == "Haferflocken").Gramm);
        Assert.Equal(2, liste.Gaesteportionen);
    }

    // ── Und was sie nicht bewegen ───────────────────────

    /// <summary>
    /// Abnahmekriterium 3, das Herzstueck: die Tagessumme zaehlt ausschliesslich
    /// die eigene Portion. Wer Gaeste eintraegt, darf seine Bilanz nicht
    /// verlieren — das war der Anlass des ganzen Merkmals.
    /// </summary>
    [Fact]
    public void Die_Tagessumme_ignoriert_Gaeste()
    {
        var ohne = MitPlan(("Mo", "mittag", "chili", 1));
        var mit = ohne with { GaesteTag = new Dictionary<string, int> { ["Mo"] = 3 } };

        var planung = Planung();

        Assert.Equal(planung.Tagessumme(ohne, "Mo", [Chili]), planung.Tagessumme(mit, "Mo", [Chili]));
        Assert.Equal((800, 50), planung.Tagessumme(mit, "Mo", [Chili]));
    }

    // Abnahmekriterium 7: automatisch fuellen fasst nur Gerichte an.
    [Fact]
    public void Automatisch_fuellen_laesst_die_Gaeste_stehen()
    {
        IReadOnlyList<Rezept> auswahl =
        [
            Oats,
            Chili,
            new Rezept("ofen", "Ofengemuese", "abend", 30, false, true, 600, 35,
                [new Zutat("Karotte", 100, "Konserven")], "Backen.")
        ];

        var vorher = WochenStand.Leer with
        {
            GaesteTag = new Dictionary<string, int> { ["Sa"] = 2 },
            GaesteMahlzeit = new Dictionary<string, int> { ["Sa|fruehstueck"] = 0 }
        };

        var nachher = Planung().AutomatischFuellen(vorher, auswahl, Ziel);

        Assert.Equal(2, nachher.Gaeste("Sa", "mittag"));
        Assert.Equal(0, nachher.Gaeste("Sa", "fruehstueck"));
    }

    private static readonly Bilanz Ziel = new(
        Grundumsatz: 1755, Alltag: 2246, SportSchnitt: 45, Gesamt: 2291,
        Defizit: 600, PhasenDefizit: 600, EigenesTempo: false,
        Normal: 1900, Refeed: 2291, Protein: 150, Wochendefizit: 4200,
        KgProWoche: 0.55, PhasenTempo: 0.55, Sport: new Sportwoche([], 0));
}
