using Microsoft.Extensions.DependencyInjection;
using Weekplan.Client.Dienste;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Core.Tagebuch.Contracts;
using Weekplan.Core.Wochenplanung;
using Weekplan.Core.Wochenplanung.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Akzeptanzkriterium 5: aendert sich ein geplantes Rezept, sieht der Nutzer es
/// mit altem und neuem Wert. Das Tabu dahinter lautet „Zahlen verschieben sich
/// still" — gerechnet wird trotzdem immer mit dem aktuellen Rezept, sonst
/// stuenden Zahlen von gestern neben Zutaten von heute auf der Einkaufsliste.
/// </summary>
public class AbweichungTests
{
    private static readonly Zutat[] Zutaten = [new("Kidneybohnen", 150, "Konserven")];

    private static Rezept Chili(int kcal = 829, int protein = 52)
        => new("chili", "Chili sin Carne", "mittag", 40, true, false, kcal, protein, Zutaten, "Kochen.");

    [Fact]
    public void Ein_unveraendertes_Rezept_meldet_keine_Abweichung()
    {
        Assert.Null(Abweichung.Zwischen(new PlanEintrag("chili", 1, 829, 52), Chili()));
    }

    [Fact]
    public void Geaenderte_Kalorien_melden_alten_und_neuen_Wert()
    {
        var abweichung = Abweichung.Zwischen(new PlanEintrag("chili", 2, 829, 52), Chili(kcal: 760));

        Assert.NotNull(abweichung);
        Assert.Equal(829, abweichung.AltKcal);
        Assert.Equal(760, abweichung.NeuKcal);
        Assert.Equal(52, abweichung.AltProtein);
        Assert.Equal(52, abweichung.NeuProtein);
    }

    [Fact]
    public void Auch_ein_geaendertes_Protein_allein_faellt_auf()
    {
        Assert.NotNull(Abweichung.Zwischen(new PlanEintrag("chili", 1, 829, 52), Chili(protein: 48)));
    }

    /// <summary>
    /// Ein Plan, der vor dieser Aenderung gespeichert wurde, hat keine gemerkten
    /// Zahlen. Er muss weiter lesbar sein — und darf keinen Hinweis erfinden.
    /// </summary>
    [Fact]
    public void Ein_alter_Eintrag_ohne_gemerkte_Zahlen_meldet_nichts()
    {
        Assert.Null(Abweichung.Zwischen(new PlanEintrag("chili", 1), Chili(kcal: 760)));
    }

    /// <summary>Ein geloeschtes Rezept ist kein Fall fuer den Aenderungshinweis, sondern fuer „entfernt".</summary>
    [Fact]
    public void Ein_geloeschtes_Rezept_meldet_keine_Abweichung()
    {
        Assert.Null(Abweichung.Zwischen(new PlanEintrag("chili", 1, 829, 52), rezept: null));
    }

    /// <summary>
    /// Die automatische Wochenfuellung muss die Zahlen mitschreiben — sonst
    /// bliebe der haeufigste Weg, eine Woche zu planen, ohne jeden Hinweis.
    /// </summary>
    [Fact]
    public void Die_automatische_Fuellung_merkt_sich_die_Zahlen()
    {
        var planung = new ServiceCollection().AddWochenplanung().BuildServiceProvider()
            .GetRequiredService<IWochenplanung>();

        var rezepte = new List<Rezept>
        {
            new("f1", "Oats", "fruehstueck", 5, true, false, 500, 30, Zutaten, "."),
            new("m1", "Chili", "mittag", 40, false, false, 800, 50, Zutaten, "."),
            new("a1", "Ofen", "abend", 35, false, false, 600, 35, Zutaten, ".")
        };

        var bilanz = new Bilanz(1600, 400, 300, 2300, 500, 500, false,
            1800, 2100, 140, 3500, 0.5, 0.5, new Sportwoche([], 1));

        var woche = planung.AutomatischFuellen(WochenStand.Leer, rezepte, bilanz);

        var eintrag = woche.Plan["Mo"]["mittag"][0];
        Assert.Equal(800, eintrag.KcalBeimPlanen);
        Assert.Equal(50, eintrag.ProteinBeimPlanen);
    }
}
