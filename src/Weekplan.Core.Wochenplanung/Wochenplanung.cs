using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Tagebuch.Contracts;
using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Core.Wochenplanung.Contracts;

namespace Weekplan.Core.Wochenplanung;

internal sealed class Wochenplanung : IWochenplanung
{
    public Einkaufsliste Einkaufsliste(
        WochenStand woche, IReadOnlyList<Rezept> rezepte, IReadOnlyList<string> abteilungen)
    {
        var nachId = rezepte.ToDictionary(r => r.Id);
        var gesammelt = new Dictionary<string, Sammler>();
        var vorratUebersprungen = 0;

        foreach (var (rezept, portionen) in Geplant(woche, nachId))
        {
            foreach (var zutat in rezept.Zutaten)
            {
                if (zutat.Vorrat)
                {
                    vorratUebersprungen++;
                    continue;
                }

                var schluessel = zutat.Name + "|" + zutat.Abt;
                if (!gesammelt.TryGetValue(schluessel, out var sammler))
                {
                    gesammelt[schluessel] = sammler = new Sammler(zutat.Name, zutat.Abt);
                }

                sammler.Gramm += zutat.G * portionen;
                sammler.Stueck += zutat.Stk * portionen;
                sammler.Quellen.Add(rezept.Name);
            }
        }

        // Nach Abteilung in der vorgegebenen Reihenfolge — so laeuft man den Laden
        // einmal ab, statt zwischen den Regalen hin und her zu springen.
        var reihenfolge = abteilungen
            .Select((name, i) => (name, i))
            .ToDictionary(x => x.name, x => x.i);

        var posten = gesammelt.Values
            .OrderBy(s => reihenfolge.TryGetValue(s.Abteilung, out var i) ? i : int.MaxValue)
            .ThenBy(s => s.Name, StringComparer.CurrentCulture)
            .Select(s => new Einkaufsposten(s.Name, s.Abteilung, s.Gramm, s.Stueck, [.. s.Quellen]))
            .ToList();

        return new Einkaufsliste(posten, vorratUebersprungen);
    }

    public (int Kcal, int Protein) Tagessumme(
        WochenStand woche, string tag, IReadOnlyList<Rezept> rezepte)
    {
        var nachId = rezepte.ToDictionary(r => r.Id);
        var kcal = 0;
        var protein = 0;

        foreach (var mahlzeit in Contracts.Woche.Mahlzeiten)
        {
            foreach (var eintrag in Eintraege(woche, tag, mahlzeit.Schluessel))
            {
                if (!nachId.TryGetValue(eintrag.RezeptId, out var rezept)) continue;
                kcal += rezept.Kcal * eintrag.Portionen;
                protein += rezept.Protein * eintrag.Portionen;
            }
        }

        return (kcal, protein);
    }

    public WochenStand AutomatischFuellen(
        WochenStand woche, IReadOnlyList<Rezept> rezepte, Bilanz bilanz)
    {
        var fruehstueck = Auswahl(rezepte, "fruehstueck");
        var mittag = Auswahl(rezepte, "mittag");
        var abend = Auswahl(rezepte, "abend");
        if (fruehstueck.Count == 0 || mittag.Count == 0 || abend.Count == 0) return woche;

        var rotation = (woche.Rotation + 1) % 1000;
        var benutzt = new Dictionary<string, int>();
        var plan = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>();

        for (var i = 0; i < Contracts.Woche.Tage.Count; i++)
        {
            var tag = Contracts.Woche.Tage[i];
            var ziel = tag.Kuerzel == woche.RefeedTag ? bilanz.Refeed : bilanz.Normal;

            // Das Fruehstueck rotiert fest — es soll bewusst gleichfoermig sein.
            var f = fruehstueck[(i + rotation) % fruehstueck.Count];

            // Der Alltag steht auf zwei sonntags vorgekochten Sorten: werktags
            // kommt die Box auf den Tisch, am Wochenende wird gekocht.
            var werktag = tag.Kuerzel is not ("Sa" or "So");
            var beste = Bestes(
                f, Vorkochbar(mittag, werktag), Vorkochbar(abend, werktag),
                ziel, bilanz.Protein, benutzt);

            benutzt[beste.Mittag.Id] = benutzt.GetValueOrDefault(beste.Mittag.Id) + 1;
            benutzt[beste.Abend.Id] = benutzt.GetValueOrDefault(beste.Abend.Id) + 1;

            plan[tag.Kuerzel] = new Dictionary<string, IReadOnlyList<PlanEintrag>>
            {
                // Die Naehrwerte werden mitgeschrieben, damit ein spaeter
                // geaendertes Rezept am betroffenen Tag auffaellt.
                ["fruehstueck"] = [Eintrag(f, beste.PortionenF)],
                ["mittag"] = [Eintrag(beste.Mittag, beste.PortionenM)],
                ["abend"] = [Eintrag(beste.Abend, beste.PortionenA)]
            };
        }

        return woche with { Plan = plan, Rotation = rotation };
    }

    /// <summary>
    /// Werktags kommen nur vorkochbare Gerichte in die Auswahl — gibt es keine,
    /// bleibt die volle. Ein Filter und keine Strafpunkte: gegen den Aufschlag
    /// fuer Wiederholung tariert, kippte eine Strafe je nach Wochentag mal so
    /// und mal so, und niemand koennte vorhersagen, was der Knopf tut. Der
    /// Alltag steht ohnehin auf zwei Sorten fuer je zwei bis drei Tage —
    /// Wiederholung ist dort der Normalfall, nicht der Makel.
    /// </summary>
    private static IReadOnlyList<Rezept> Vorkochbar(IReadOnlyList<Rezept> auswahl, bool werktag)
    {
        if (!werktag) return auswahl;

        List<Rezept> vorkochbar = [.. auswahl.Where(r => r.Prep)];
        return vorkochbar.Count > 0 ? vorkochbar : auswahl;
    }

    private static (Rezept Mittag, Rezept Abend, int PortionenF, int PortionenM, int PortionenA) Bestes(
        Rezept fruehstueck,
        IReadOnlyList<Rezept> mittag,
        IReadOnlyList<Rezept> abend,
        int zielKcal,
        int zielProtein,
        Dictionary<string, int> benutzt)
    {
        var besteBewertung = double.PositiveInfinity;
        var ergebnis = (Mittag: mittag[0], Abend: abend[0], F: 1, M: 1, A: 1);

        foreach (var m in mittag)
        {
            foreach (var a in abend)
            {
                for (var fp = 1; fp <= 2; fp++)
                for (var mp = 1; mp <= 2; mp++)
                for (var ap = 1; ap <= 2; ap++)
                {
                    var kcal = fruehstueck.Kcal * fp + m.Kcal * mp + a.Kcal * ap;
                    var protein = fruehstueck.Protein * fp + m.Protein * mp + a.Protein * ap;

                    // Kalorienabstand zaehlt einfach; fehlendes Protein wiegt schwer,
                    // Wiederholung ebenso, und grosse Portionen kosten einen Aufschlag.
                    var bewertung = Math.Abs(kcal - zielKcal)
                        + Math.Max(0, zielProtein - protein) * 12
                        + (benutzt.GetValueOrDefault(m.Id) + benutzt.GetValueOrDefault(a.Id)) * 250
                        + (fp - 1 + (mp - 1) + (ap - 1)) * 20;

                    if (bewertung >= besteBewertung) continue;
                    besteBewertung = bewertung;
                    ergebnis = (m, a, fp, mp, ap);
                }
            }
        }

        return (ergebnis.Mittag, ergebnis.Abend, ergebnis.F, ergebnis.M, ergebnis.A);
    }

    private static List<Rezept> Auswahl(IReadOnlyList<Rezept> rezepte, string kategorie)
        => [.. rezepte.Where(r => r.Kategorie == kategorie)];

    private static IEnumerable<(Rezept Rezept, int Portionen)> Geplant(
        WochenStand woche, Dictionary<string, Rezept> nachId)
    {
        foreach (var tag in Contracts.Woche.Tage)
        {
            foreach (var mahlzeit in Contracts.Woche.Mahlzeiten)
            {
                foreach (var eintrag in Eintraege(woche, tag.Kuerzel, mahlzeit.Schluessel))
                {
                    if (nachId.TryGetValue(eintrag.RezeptId, out var rezept))
                    {
                        yield return (rezept, eintrag.Portionen);
                    }
                }
            }
        }
    }

    /// <summary>Ein Planeintrag, der sich merkt, mit welchen Zahlen geplant wurde.</summary>
    private static PlanEintrag Eintrag(Rezept rezept, int portionen)
        => new(rezept.Id, portionen, rezept.Kcal, rezept.Protein);

    private static IReadOnlyList<PlanEintrag> Eintraege(WochenStand woche, string tag, string mahlzeit)
        => woche.Plan.TryGetValue(tag, out var mahlzeiten)
           && mahlzeiten.TryGetValue(mahlzeit, out var eintraege)
            ? eintraege
            : [];

    private sealed class Sammler(string name, string abteilung)
    {
        public string Name { get; } = name;
        public string Abteilung { get; } = abteilung;
        public double Gramm { get; set; }
        public double Stueck { get; set; }
        public SortedSet<string> Quellen { get; } = [];
    }
}
