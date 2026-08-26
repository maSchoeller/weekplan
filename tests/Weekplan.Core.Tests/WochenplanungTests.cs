using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Tagebuch.Contracts;
using Weekplan.Core.Wochenplanung;
using Weekplan.Core.Wochenplanung.Contracts;

namespace Weekplan.Core.Tests;

public class WochenplanungTests
{
    private static IWochenplanung Planung() =>
        new ServiceCollection().AddWochenplanung().BuildServiceProvider()
            .GetRequiredService<IWochenplanung>();

    private static readonly string[] Abteilungen = ["Obst & Gemuese", "Konserven", "Oel & Gewuerze"];

    private static readonly Rezept Chili = new(
        "chili", "Chili sin Carne", "mittag", 40, true, 800, 50,
        [
            new Zutat("Kidneybohnen", 150, "Konserven"),
            new Zutat("Zwiebel", 70, "Obst & Gemuese"),
            new Zutat("Olivenoel", 6, "Oel & Gewuerze", Vorrat: true)
        ],
        ["Kochen."]);

    private static readonly Rezept Ofen = new(
        "ofen", "Ofengemuese", "abend", 35, false, 600, 30,
        [new Zutat("Zwiebel", 30, "Obst & Gemuese"), new Zutat("Karotte", 100, "Obst & Gemuese")],
        ["Backen."]);

    private static readonly Rezept Oats = new(
        "oats", "Overnight Oats", "fruehstueck", 5, true, 500, 30,
        [new Zutat("Haferflocken", 80, "Trockenware"), new Zutat("Ei", 0, "Kuehlregal", Stk: 1)],
        ["Ruehren."]);

    private static WochenStand MitPlan(params (string Tag, string Mahlzeit, string RezeptId, int Portionen)[] eintraege)
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

    // Akzeptanzkriterium 3: die Einkaufsliste in Gramm.
    [Fact]
    public void Zutaten_werden_ueber_die_Woche_summiert()
    {
        var woche = MitPlan(("Mo", "mittag", "chili", 2), ("Di", "abend", "ofen", 1));

        var liste = Planung().Einkaufsliste(woche, [Chili, Ofen], Abteilungen);

        var zwiebel = liste.Posten.Single(p => p.Name == "Zwiebel");
        Assert.Equal(170, zwiebel.Gramm);      // 70 × 2 + 30 × 1
        Assert.Equal(300, liste.Posten.Single(p => p.Name == "Kidneybohnen").Gramm);
    }

    [Fact]
    public void Ein_Posten_nennt_die_Gerichte_aus_denen_er_kommt()
    {
        var woche = MitPlan(("Mo", "mittag", "chili", 1), ("Di", "abend", "ofen", 1));

        var zwiebel = Planung().Einkaufsliste(woche, [Chili, Ofen], Abteilungen).Posten
            .Single(p => p.Name == "Zwiebel");

        Assert.Equal(["Chili sin Carne", "Ofengemuese"], zwiebel.Quellen.Order());
    }

    // Vorratsware steht im Grundstock — 6 g Olivenoel auf der Wochenliste sind Unsinn.
    [Fact]
    public void Vorratsware_bleibt_draussen_und_wird_gezaehlt()
    {
        var liste = Planung().Einkaufsliste(MitPlan(("Mo", "mittag", "chili", 1)), [Chili], Abteilungen);

        Assert.DoesNotContain(liste.Posten, p => p.Name == "Olivenoel");
        Assert.Equal(1, liste.VorratUebersprungen);
    }

    [Fact]
    public void Was_in_Stueck_gezaehlt_wird_bleibt_in_Stueck()
    {
        var liste = Planung().Einkaufsliste(MitPlan(("Mo", "fruehstueck", "oats", 3)), [Oats], Abteilungen);

        var ei = liste.Posten.Single(p => p.Name == "Ei");
        Assert.True(ei.InStueck);
        Assert.Equal(3, ei.Stueck);
    }

    [Fact]
    public void Die_Liste_folgt_der_Reihenfolge_der_Abteilungen()
    {
        var woche = MitPlan(("Mo", "mittag", "chili", 1));

        var abteilungen = Planung().Einkaufsliste(woche, [Chili], Abteilungen).Posten
            .Select(p => p.Abteilung).Distinct().ToList();

        Assert.Equal(["Obst & Gemuese", "Konserven"], abteilungen);
    }

    [Fact]
    public void Ein_unbekanntes_Rezept_im_Plan_wirft_nicht()
    {
        var liste = Planung().Einkaufsliste(MitPlan(("Mo", "mittag", "gibtsnicht", 1)), [Chili], Abteilungen);

        Assert.Empty(liste.Posten);
    }

    [Fact]
    public void Tagessumme_zaehlt_Portionen_mit()
    {
        var woche = MitPlan(("Mo", "fruehstueck", "oats", 1), ("Mo", "mittag", "chili", 2));

        var (kcal, protein) = Planung().Tagessumme(woche, "Mo", [Oats, Chili]);

        Assert.Equal(500 + 1600, kcal);
        Assert.Equal(30 + 100, protein);
    }
}

public class AutomatischFuellenTests
{
    private static IWochenplanung Planung() =>
        new ServiceCollection().AddWochenplanung().BuildServiceProvider()
            .GetRequiredService<IWochenplanung>();

    private static Rezept R(string id, string kategorie, int kcal, int protein) =>
        new(id, id, kategorie, 20, false, kcal, protein, [new Zutat("X", 10, "Trockenware")], ["Kochen."]);

    private static readonly List<Rezept> Auswahl =
    [
        R("f1", "fruehstueck", 500, 30), R("f2", "fruehstueck", 450, 25),
        R("m1", "mittag", 800, 50), R("m2", "mittag", 700, 45),
        R("a1", "abend", 600, 35), R("a2", "abend", 550, 30)
    ];

    private static readonly Bilanz Ziel = new(
        Grundumsatz: 1755, Alltag: 2246, SportSchnitt: 45, Gesamt: 2291,
        Defizit: 600, PhasenDefizit: 600, EigenesTempo: false,
        Normal: 1900, Refeed: 2291, Protein: 150, Wochendefizit: 4200,
        KgProWoche: 0.55, PhasenTempo: 0.55, Sport: new Sportwoche([], 0));

    [Fact]
    public void Jeder_Tag_bekommt_drei_Mahlzeiten()
    {
        var voll = Planung().AutomatischFuellen(WochenStand.Leer, Auswahl, Ziel);

        foreach (var tag in Woche.Tage)
        {
            foreach (var mahlzeit in Woche.Mahlzeiten)
            {
                Assert.Single(voll.Plan[tag.Kuerzel][mahlzeit.Schluessel]);
            }
        }
    }

    [Fact]
    public void Jeder_Tag_landet_nah_am_Kalorienziel()
    {
        var planung = Planung();
        var voll = planung.AutomatischFuellen(WochenStand.Leer, Auswahl, Ziel);

        foreach (var tag in Woche.Tage)
        {
            var ziel = tag.Kuerzel == voll.RefeedTag ? Ziel.Refeed : Ziel.Normal;
            var (kcal, _) = planung.Tagessumme(voll, tag.Kuerzel, Auswahl);

            Assert.InRange(kcal, ziel - 400, ziel + 400);
        }
    }

    [Fact]
    public void Zweimal_fuellen_ergibt_nicht_dieselbe_Woche()
    {
        var planung = Planung();

        var erste = planung.AutomatischFuellen(WochenStand.Leer, Auswahl, Ziel);
        var zweite = planung.AutomatischFuellen(erste, Auswahl, Ziel);

        Assert.NotEqual(erste.Rotation, zweite.Rotation);
        Assert.NotEqual(
            erste.Plan["Mo"]["fruehstueck"].Single().RezeptId,
            zweite.Plan["Mo"]["fruehstueck"].Single().RezeptId);
    }

    [Fact]
    public void Ohne_Rezepte_einer_Kategorie_bleibt_der_Plan_wie_er_war()
    {
        var nurFruehstueck = Auswahl.Where(r => r.Kategorie == "fruehstueck").ToList();

        var ergebnis = Planung().AutomatischFuellen(WochenStand.Leer, nurFruehstueck, Ziel);

        Assert.Empty(ergebnis.Plan);
    }
}
