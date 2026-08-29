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
        var gaesteportionen = 0;

        foreach (var (rezept, portionen, gaeste) in Geplant(woche, nachId))
        {
            gaesteportionen += gaeste;

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

        return new Einkaufsliste(posten, vorratUebersprungen, gaesteportionen);
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

    /// <summary>
    /// Fuellt die Woche nach den Regeln aus <c>docs/ernaehrungsplan.md</c> §3:
    /// werktags zwei vorgekochte Sorten in zusammenhaengenden Bloecken, am
    /// Wochenende frisch gekocht, am Refeed-Tag eigene Gerichte, das Fruehstueck
    /// taeglich rotierend.
    ///
    /// <para>
    /// <b>Erst filtern, dann bewerten.</b> Jede Regel ist eine Vorauswahl mit
    /// Rueckfall, kein Strafpunkt in einer Bewertungsfunktion — Strafpunkte
    /// kaempfen gegeneinander und machen aus einer Regel ein Raetsel. Innerhalb
    /// der Vorauswahl entscheidet weiterhin der Abstand zum Kalorienziel und das
    /// fehlende Protein.
    /// </para>
    ///
    /// <para>
    /// Die bisherige Woche wird <b>ueberbuegelt, nicht ausgewertet</b>. Die
    /// Abwechslung beim zweiten Druecken traegt allein <c>Rotation</c>.
    /// </para>
    /// </summary>
    public WochenStand AutomatischFuellen(
        WochenStand woche, IReadOnlyList<Rezept> rezepte, Bilanz bilanz)
    {
        var fruehstueck = Auswahl(rezepte, "fruehstueck");
        var mittag = Auswahl(rezepte, "mittag");
        var abend = Auswahl(rezepte, "abend");
        if (fruehstueck.Count == 0 || mittag.Count == 0 || abend.Count == 0) return woche;

        var rotation = (woche.Rotation + 1) % 1000;
        var tage = Contracts.Woche.Tage;

        // Das Fruehstueck rotiert taeglich — die Mahlzeit, die Wiederholung am
        // besten vertraegt, und vorzubereiten ist daran nichts.
        var fruehJeTag = Enumerable.Range(0, tage.Count)
            .Select(i => fruehstueck[(i + rotation) % fruehstueck.Count])
            .ToArray();

        var mittagJeTag = new Rezept[tage.Count];
        var abendJeTag = new Rezept[tage.Count];
        var vergeben = new HashSet<string>();

        // Der Refeed-Tag gewinnt gegen jede andere Regel; danach das Wochenende.
        // Beide werden Tag fuer Tag gewaehlt, nicht in Bloecken — dort wird
        // frisch gekocht.
        foreach (var i in Einzeltage(woche.RefeedTag))
        {
            var refeed = tage[i].Kuerzel == woche.RefeedTag;
            var m = Gefiltert(mittag, r => refeed ? r.Refeed : r.Wochenende);
            var a = Gefiltert(abend, r => refeed ? r.Refeed : r.Wochenende);

            var paar = BestesPaar(
                Ohne(m, vergeben), Ohne(a, vergeben),
                [(fruehJeTag[i], Ziel(tage[i].Kuerzel))], bilanz, rotation);

            mittagJeTag[i] = paar.Mittag;
            abendJeTag[i] = paar.Abend;
            vergeben.Add(paar.Mittag.Id);
            vergeben.Add(paar.Abend.Id);
        }

        // Die Werktage stehen auf zwei sonntags vorgekochten Sorten. Ein Gericht
        // je Block, und der zweite Block ein anderes als der erste.
        var werktage = Werktage(woche.RefeedTag);
        // Die Bloecke wissen, was Wochenende und Refeed schon bekommen haben:
        // ein Gericht, das zugleich vorkochbar und Wochenendgericht ist, stuende
        // sonst zweimal in derselben Woche.
        var werktagsMittag = Gefiltert(mittag, r => r.Prep);
        var werktagsAbend = Gefiltert(abend, r => r.Prep);
        var genommen = new HashSet<string>(vergeben);
        var ab = 0;

        foreach (var laenge in Blockaufteilung(werktage.Length, rotation))
        {
            var block = werktage.Skip(ab).Take(laenge).ToArray();
            ab += laenge;

            var paar = BestesPaar(
                Ohne(werktagsMittag, genommen), Ohne(werktagsAbend, genommen),
                [.. block.Select(i => (fruehJeTag[i], Ziel(tage[i].Kuerzel)))], bilanz, rotation);

            foreach (var i in block)
            {
                mittagJeTag[i] = paar.Mittag;
                abendJeTag[i] = paar.Abend;
            }

            genommen.Add(paar.Mittag.Id);
            genommen.Add(paar.Abend.Id);
        }

        var plan = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>();

        for (var i = 0; i < tage.Count; i++)
        {
            var portionen = BestePortionen(
                fruehJeTag[i], mittagJeTag[i], abendJeTag[i], Ziel(tage[i].Kuerzel), bilanz.Protein);

            plan[tage[i].Kuerzel] = new Dictionary<string, IReadOnlyList<PlanEintrag>>
            {
                // Die Naehrwerte werden mitgeschrieben, damit ein spaeter
                // geaendertes Rezept am betroffenen Tag auffaellt.
                ["fruehstueck"] = [Eintrag(fruehJeTag[i], portionen.F)],
                ["mittag"] = [Eintrag(mittagJeTag[i], portionen.M)],
                ["abend"] = [Eintrag(abendJeTag[i], portionen.A)]
            };
        }

        // Gaeste bleiben stehen: der Besuch ist eine Tatsache und haengt nicht
        // am Plan. `with` fasst nur an, was hier genannt wird.
        return woche with { Plan = plan, Rotation = rotation };

        int Ziel(string kuerzel) => kuerzel == woche.RefeedTag ? bilanz.Refeed : bilanz.Normal;
    }

    /// <summary>
    /// Die Kehrseite des Rueckfalls, ausgesprochen. Geprueft wird der Pool und
    /// nicht das Ergebnis: eine gefuellte Woche darf der Nutzer jederzeit von
    /// Hand umbauen, und ein Hinweis, der dann weiter mahnt, waere Bevormundung.
    /// Was fehlt, fehlt am Pool — und dort wird es auch behoben.
    /// </summary>
    public IReadOnlyList<string> Fuellhinweise(IReadOnlyList<Rezept> rezepte)
    {
        // Fruehstuecke rotieren und kennen keine dieser Regeln.
        var warm = rezepte.Where(r => r.Kategorie is "mittag" or "abend").ToList();
        if (warm.Count == 0) return [];

        var hinweise = new List<string>();

        if (!warm.Any(r => r.Prep))
        {
            hinweise.Add("Kein Gericht ist als vorkochbar markiert — werktags steht "
                         + "darum die volle Auswahl statt zwei vorgekochter Sorten.");
        }

        if (!warm.Any(r => r.Wochenende))
        {
            hinweise.Add("Kein Gericht ist als Wochenendgericht markiert — Samstag "
                         + "und Sonntag bekommen darum dieselbe Auswahl wie die Werktage.");
        }

        if (!warm.Any(r => r.Refeed))
        {
            hinweise.Add("Kein Gericht ist als refeed-tauglich markiert — der "
                         + "Refeed-Tag bekommt darum gewöhnliche Gerichte in größeren Portionen.");
        }

        return hinweise;
    }

    /// <summary>Refeed-Tag zuerst, danach die uebrigen Wochenendtage — jeder fuer sich gewaehlt.</summary>
    private static IEnumerable<int> Einzeltage(string refeedTag)
    {
        var tage = Contracts.Woche.Tage;

        for (var i = 0; i < tage.Count; i++)
        {
            if (tage[i].Kuerzel == refeedTag) yield return i;
        }

        for (var i = 0; i < tage.Count; i++)
        {
            if (tage[i].Kuerzel is "Sa" or "So" && tage[i].Kuerzel != refeedTag) yield return i;
        }
    }

    private static int[] Werktage(string refeedTag)
    {
        var tage = Contracts.Woche.Tage;
        return [.. Enumerable.Range(0, tage.Count)
            .Where(i => tage[i].Kuerzel is not ("Sa" or "So") && tage[i].Kuerzel != refeedTag)];
    }

    /// <summary>
    /// Zerlegt die Werktage in zusammenhaengende Bloecke von zwei bis drei Tagen
    /// — die Laenge, die ein vorgekochtes Gericht im Kuehlschrank durchhaelt.
    /// Bei fuenf Tagen entscheidet die Rotation, ob der lange Block vorne oder
    /// hinten liegt.
    /// </summary>
    private static int[] Blockaufteilung(int werktage, int rotation) => werktage switch
    {
        0 => [],
        <= 3 => [werktage],
        4 => [2, 2],
        _ => rotation % 2 == 0 ? [3, werktage - 3] : [2, werktage - 2]
    };

    /// <summary>
    /// Eine Vorauswahl mit Rueckfall: bleibt nach dem Filter nichts uebrig, gilt
    /// die volle Auswahl. So bleibt kein Tag leer, auch wenn ein Merkmal an
    /// keinem Gericht gepflegt ist — und genau das traegt den Pool direkt nach
    /// dem Ausrollen.
    /// </summary>
    private static IReadOnlyList<Rezept> Gefiltert(
        IReadOnlyList<Rezept> auswahl, Func<Rezept, bool> merkmal)
    {
        List<Rezept> passend = [.. auswahl.Where(merkmal)];
        return passend.Count > 0 ? passend : auswahl;
    }

    private static IReadOnlyList<Rezept> Ohne(IReadOnlyList<Rezept> auswahl, HashSet<string> schon)
        => Gefiltert(auswahl, r => !schon.Contains(r.Id));

    /// <summary>
    /// Das Paar (Mittag, Abend), das ueber alle Tage der Gruppe zusammen am
    /// besten passt. Zurueckgegeben wird nicht das beste, sondern das
    /// <c>rotation % 3</c>-beste: alle stammen aus derselben Vorauswahl, sind
    /// also gleich regelkonform, und treffen das Kalorienziel nur verschieden
    /// genau. Das ist die ganze Abwechslung beim zweiten Druecken.
    /// </summary>
    private static (Rezept Mittag, Rezept Abend) BestesPaar(
        IReadOnlyList<Rezept> mittag,
        IReadOnlyList<Rezept> abend,
        IReadOnlyList<(Rezept Fruehstueck, int Ziel)> tage,
        Bilanz bilanz,
        int rotation)
    {
        var bewertet = new List<(Rezept M, Rezept A, double Punkte)>();

        foreach (var m in mittag)
        {
            foreach (var a in abend)
            {
                var punkte = tage.Sum(t =>
                    BestePortionen(t.Fruehstueck, m, a, t.Ziel, bilanz.Protein).Punkte);

                bewertet.Add((m, a, punkte));
            }
        }

        // Nach Kennung als zweitem Schluessel, damit die Reihenfolge des Pools
        // das Ergebnis nicht verschiebt.
        var geordnet = bewertet
            .OrderBy(x => x.Punkte)
            .ThenBy(x => x.M.Id, StringComparer.Ordinal)
            .ThenBy(x => x.A.Id, StringComparer.Ordinal)
            .ToList();

        var gewaehlt = geordnet[Math.Min(rotation % 3, geordnet.Count - 1)];
        return (gewaehlt.M, gewaehlt.A);
    }

    /// <summary>
    /// Die Portionen eines Tages bei feststehenden Gerichten. Kalorienabstand
    /// zaehlt einfach, fehlendes Protein wiegt schwer, grosse Portionen kosten
    /// einen Aufschlag. Ein Aufschlag fuer Wiederholung steht hier nicht mehr:
    /// Wiederholung ist seit den Bloecken Struktur, nicht Makel.
    /// </summary>
    private static (int F, int M, int A, double Punkte) BestePortionen(
        Rezept fruehstueck, Rezept mittag, Rezept abend, int zielKcal, int zielProtein)
    {
        var beste = double.PositiveInfinity;
        var ergebnis = (F: 1, M: 1, A: 1);

        for (var fp = 1; fp <= 2; fp++)
        for (var mp = 1; mp <= 2; mp++)
        for (var ap = 1; ap <= 2; ap++)
        {
            var kcal = fruehstueck.Kcal * fp + mittag.Kcal * mp + abend.Kcal * ap;
            var protein = fruehstueck.Protein * fp + mittag.Protein * mp + abend.Protein * ap;

            var punkte = Math.Abs(kcal - zielKcal)
                + Math.Max(0, zielProtein - protein) * 12
                + (fp - 1 + (mp - 1) + (ap - 1)) * 20;

            if (punkte >= beste) continue;
            beste = punkte;
            ergebnis = (fp, mp, ap);
        }

        return (ergebnis.F, ergebnis.M, ergebnis.A, beste);
    }

    private static List<Rezept> Auswahl(IReadOnlyList<Rezept> rezepte, string kategorie)
        => [.. rezepte.Where(r => r.Kategorie == kategorie)];

    /// <summary>
    /// Jedes geplante Gericht mit seinen <b>Kochportionen</b> — eigene Portionen
    /// plus zusaetzliche Esser. Nur der Einkauf laeuft hier durch; die Bilanz
    /// nimmt in <see cref="Tagessumme"/> ihren eigenen Weg und sieht die
    /// Gaestezahl nie.
    /// </summary>
    private static IEnumerable<(Rezept Rezept, int Portionen, int Gaeste)> Geplant(
        WochenStand woche, Dictionary<string, Rezept> nachId)
    {
        foreach (var tag in Contracts.Woche.Tage)
        {
            foreach (var mahlzeit in Contracts.Woche.Mahlzeiten)
            {
                var gaeste = woche.Gaeste(tag.Kuerzel, mahlzeit.Schluessel);

                foreach (var eintrag in Eintraege(woche, tag.Kuerzel, mahlzeit.Schluessel))
                {
                    if (nachId.TryGetValue(eintrag.RezeptId, out var rezept))
                    {
                        yield return (rezept, eintrag.Portionen + gaeste, gaeste);
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
