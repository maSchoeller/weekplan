using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Core.Tagebuch.Contracts;
using Weekplan.Core.Wochenplanung;
using Weekplan.Core.Wochenplanung.Contracts;
using Altbestand = Weekplan.Stammdaten.Altbestand;

namespace Weekplan.Core.Tests;

/// <summary>
/// Akzeptanzkriterium 9: der Umzug ist verlustfrei. Geprueft wird gegen die
/// eingefrorenen JSON-Dateien selbst — und zwar mit einem <b>zweiten</b>,
/// unabhaengigen Leser (<see cref="JsonDocument"/>). Wuerde derselbe Leser
/// beide Seiten liefern, prueften wir nur, dass er mit sich selbst
/// uebereinstimmt.
/// </summary>
public class UmzugTests
{
    private static string Altbestandsordner => Path.Combine(AppContext.BaseDirectory, "altbestand");

    private static JsonElement Roh()
    {
        var text = File.ReadAllText(Path.Combine(Altbestandsordner, "rezepte.json"));
        return JsonDocument.Parse(text).RootElement;
    }

    private static async Task<Stammdatensatz> UmgezogenAsync() => await Altbestand.LesenAsync(Altbestandsordner);

    [Fact]
    public async Task Kein_Rezept_geht_verloren()
    {
        var roh = Roh().GetProperty("rezepte");
        var neu = (await UmgezogenAsync()).Rezepte.Rezepte;

        Assert.Equal(roh.GetArrayLength(), neu.Count);
        Assert.Equal(
            roh.EnumerateArray().Select(r => r.GetProperty("id").GetString()).Order(),
            neu.Select(r => r.Id).Order());
    }

    [Fact]
    public async Task Jedes_Rezept_behaelt_Name_Kategorie_Zeit_und_Naehrwerte()
    {
        var neu = (await UmgezogenAsync()).Rezepte.Rezepte.ToDictionary(r => r.Id);

        foreach (var roh in Roh().GetProperty("rezepte").EnumerateArray())
        {
            var rezept = neu[roh.GetProperty("id").GetString()!];

            Assert.Equal(roh.GetProperty("name").GetString(), rezept.Name);
            Assert.Equal(roh.GetProperty("kategorie").GetString(), rezept.Kategorie);
            Assert.Equal(roh.GetProperty("zeitMin").GetInt32(), rezept.ZeitMin);
            Assert.Equal(roh.GetProperty("kalt").GetBoolean(), rezept.Kalt);
            Assert.Equal(roh.GetProperty("kcal").GetInt32(), rezept.Kcal);
            Assert.Equal(roh.GetProperty("protein").GetInt32(), rezept.Protein);
        }
    }

    [Fact]
    public async Task Jede_Zutat_behaelt_Name_Menge_Abteilung_und_Kennzeichen()
    {
        var neu = (await UmgezogenAsync()).Rezepte.Rezepte.ToDictionary(r => r.Id);

        foreach (var roh in Roh().GetProperty("rezepte").EnumerateArray())
        {
            var id = roh.GetProperty("id").GetString()!;
            var zutaten = neu[id].Zutaten;
            var rohzutaten = roh.GetProperty("zutaten").EnumerateArray().ToList();

            Assert.Equal(rohzutaten.Count, zutaten.Count);

            for (var i = 0; i < rohzutaten.Count; i++)
            {
                var (soll, ist) = (rohzutaten[i], zutaten[i]);

                Assert.Equal(soll.GetProperty("name").GetString(), ist.Name);
                Assert.Equal(soll.GetProperty("abt").GetString(), ist.Abt);
                Assert.Equal(Zahl(soll, "g"), ist.G);
                Assert.Equal(Zahl(soll, "stk"), ist.Stk);
                Assert.Equal(soll.TryGetProperty("vorrat", out var v) && v.GetBoolean(), ist.Vorrat);
            }
        }

        static double Zahl(JsonElement e, string name)
            => e.TryGetProperty(name, out var wert) ? wert.GetDouble() : 0;
    }

    /// <summary>
    /// Die Schritte werden zu einer nummerierten Markdown-Liste: derselbe Satz,
    /// dieselbe Reihenfolge, dieselbe Nummer. Nichts wird gekuerzt oder
    /// umformuliert — das taete der Nutzer spaeter selbst.
    /// </summary>
    [Fact]
    public async Task Aus_Schritten_wird_eine_nummerierte_Anleitung_ohne_Textverlust()
    {
        var neu = (await UmgezogenAsync()).Rezepte.Rezepte.ToDictionary(r => r.Id);

        foreach (var roh in Roh().GetProperty("rezepte").EnumerateArray())
        {
            var schritte = roh.GetProperty("schritte").EnumerateArray()
                .Select(s => s.GetString()!).ToList();
            var anleitung = neu[roh.GetProperty("id").GetString()!].Anleitung;
            var zeilen = anleitung.Split('\n');

            Assert.Equal(schritte.Count, zeilen.Length);
            for (var i = 0; i < schritte.Count; i++)
            {
                Assert.Equal($"{i + 1}. {schritte[i]}", zeilen[i]);
            }
        }
    }

    /// <summary>
    /// Der Teil, den ein Fehler am leisesten traefe: die Einkaufsliste. Sie wird
    /// einmal aus den umgezogenen Rezepten gerechnet und einmal aus Rezepten,
    /// die der Test selbst aus dem rohen JSON baut — Posten fuer Posten gleich.
    /// </summary>
    [Fact]
    public async Task Die_Einkaufsliste_einer_Beispielwoche_bleibt_Posten_fuer_Posten_gleich()
    {
        var planung = new ServiceCollection().AddWochenplanung().BuildServiceProvider()
            .GetRequiredService<IWochenplanung>();

        var umgezogen = await UmgezogenAsync();
        var ausRoh = AusRohemJson();
        var woche = BeispielwocheMit(umgezogen.Rezepte.Rezepte);

        var vorher = planung.Einkaufsliste(woche, ausRoh, umgezogen.Rezepte.Abteilungen);
        var nachher = planung.Einkaufsliste(woche, umgezogen.Rezepte.Rezepte, umgezogen.Rezepte.Abteilungen);

        Assert.NotEmpty(nachher.Posten);
        Assert.Equal(vorher.VorratUebersprungen, nachher.VorratUebersprungen);
        Assert.Equal(vorher.Posten.Count, nachher.Posten.Count);

        foreach (var (soll, ist) in vorher.Posten.Zip(nachher.Posten))
        {
            Assert.Equal(soll.Name, ist.Name);
            Assert.Equal(soll.Abteilung, ist.Abteilung);
            Assert.Equal(soll.Gramm, ist.Gramm);
            Assert.Equal(soll.Stueck, ist.Stueck);
            Assert.Equal(soll.Quellen, ist.Quellen);
        }
    }

    /// <summary>Rezepte direkt aus dem rohen JSON — ohne den Umzugsweg.</summary>
    private static List<Rezept> AusRohemJson() =>
    [
        .. Roh().GetProperty("rezepte").EnumerateArray().Select(r => new Rezept(
            r.GetProperty("id").GetString()!,
            r.GetProperty("name").GetString()!,
            r.GetProperty("kategorie").GetString()!,
            r.GetProperty("zeitMin").GetInt32(),
            r.GetProperty("kalt").GetBoolean(),
            // Der Altbestand kennt kein prep — der Umzug setzt es auf false,
            // und genau das wird hier nachgestellt.
            false,
            r.GetProperty("kcal").GetInt32(),
            r.GetProperty("protein").GetInt32(),
            [.. r.GetProperty("zutaten").EnumerateArray().Select(z => new Zutat(
                z.GetProperty("name").GetString()!,
                z.TryGetProperty("g", out var g) ? g.GetDouble() : 0,
                z.GetProperty("abt").GetString()!,
                z.TryGetProperty("vorrat", out var v) && v.GetBoolean(),
                z.TryGetProperty("stk", out var s) ? s.GetDouble() : 0))],
            ""))
    ];

    /// <summary>Jedes Rezept einmal, damit kein Posten der Liste unberuehrt bleibt.</summary>
    private static WochenStand BeispielwocheMit(IReadOnlyList<Rezept> rezepte)
    {
        var plan = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>();

        foreach (var (rezept, i) in rezepte.Select((r, i) => (r, i)))
        {
            var tag = Woche.Tage[i % Woche.Tage.Count].Kuerzel;
            var mahlzeiten = plan.TryGetValue(tag, out var da)
                ? da.ToDictionary(x => x.Key, x => x.Value)
                : [];

            var vorhanden = mahlzeiten.TryGetValue(rezept.Kategorie, out var e) ? e : [];
            mahlzeiten[rezept.Kategorie] = [.. vorhanden, new PlanEintrag(rezept.Id, 2)];
            plan[tag] = mahlzeiten;
        }

        return WochenStand.Leer with { Plan = plan };
    }
}
