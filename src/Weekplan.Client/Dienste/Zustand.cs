using Weekplan.Client.Daten;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Core.Tagebuch.Contracts;
using Weekplan.Core.Wochenplanung.Contracts;
using Profil = Weekplan.Core.Rechnen.Contracts.Profil;

namespace Weekplan.Client.Dienste;

public enum Speicherlage { Ruhe, Schreibt, Fehler }

/// <summary>Ein Rezept hat sich seit dem Planen geaendert: alte und neue Zahlen je Portion.</summary>
public sealed record Abweichung(int AltKcal, int AltProtein, int NeuKcal, int NeuProtein)
{
    /// <summary>
    /// Was sich seit dem Planen geaendert hat — <c>null</c>, wenn nichts, wenn
    /// das Rezept fehlt (das ist der Fall „entfernt", nicht „geaendert"), oder
    /// wenn der Eintrag aus der Zeit vor den gemerkten Zahlen stammt.
    /// </summary>
    public static Abweichung? Zwischen(PlanEintrag eintrag, Rezept? rezept)
    {
        if (rezept is null) return null;
        if (eintrag.KcalBeimPlanen is not { } altKcal) return null;
        if (eintrag.ProteinBeimPlanen is not { } altProtein) return null;
        if (altKcal == rezept.Kcal && altProtein == rezept.Protein) return null;

        return new Abweichung(altKcal, altProtein, rezept.Kcal, rezept.Protein);
    }
}

/// <summary>
/// Der Stand des Nutzers im Browser, und der Weg zum Server. Eingaben wirken
/// sofort in der Oberflaeche; geschrieben wird gebuendelt nach kurzer Ruhe.
/// Das ist der einzige Weg, das Gewicht in zwei Sekunden einzutragen, ohne bei
/// jedem Tastendruck auf den Server zu warten.
/// </summary>
public sealed class Zustand(
    WeekplanApi api,
    Stammdatenlader lader,
    IRechner rechner,
    IWochenplanung planung)
{
    private static readonly TimeSpan Ruhefrist = TimeSpan.FromMilliseconds(800);

    private CancellationTokenSource? _profilWartet;
    private CancellationTokenSource? _wocheWartet;

    public Stammdatensatz Stamm { get; private set; } = null!;
    public ProfilStand Profil { get; private set; } = ProfilStand.Leer;
    public WochenStand Woche { get; private set; } = WochenStand.Leer;

    public Speicherlage Lage { get; private set; } = Speicherlage.Ruhe;
    public bool Geladen { get; private set; }

    /// <summary>Meldet jede Aenderung, damit die Oberflaeche neu zeichnet.</summary>
    public event Action? Geaendert;

    public IReadOnlyList<Rezept> Rezepte => Stamm.Rezepte.Rezepte;

    /// <summary>
    /// Eine Planaenderung hat die Zielaufnahme verschoben — alte und neue Zahl
    /// fuer den Normaltag.
    /// </summary>
    public sealed record Planaenderung(int Alt, int Neu);

    private Stammdatensatz? _vorherigerStand;

    /// <summary>
    /// <c>null</c>, wenn nichts anliegt oder der Nutzer es weggeklickt hat.
    ///
    /// <para>
    /// Gerechnet wird beim Lesen, nicht beim Auffrischen. Der Grund ist ein
    /// Wettlauf: <c>LadenAsync</c> stoesst das Nachfragen an und holt <b>danach</b>
    /// das Profil; gegen einen schnellen Server ist der neue Stand da, bevor ein
    /// Gewicht vorliegt — und ohne Gewicht gibt es keine Zielaufnahme. Beim
    /// Ereignis zu rechnen verschluckte den Hinweis also genau dann, wenn alles
    /// gut laeuft.
    /// </para>
    /// </summary>
    public Planaenderung? Planhinweis
    {
        get
        {
            // Beim Kaltstart ohne Browserspeicher gibt es keinen Vorgaenger und
            // damit nichts zu vergleichen. Das ist kein Hinweis, sondern Ruhe.
            if (!Geladen || _vorherigerStand is not { } vorher) return null;

            // Einen Hinweis ist nur wert, was rueckwirkend rechnet: Grundstock
            // und Abteilungen bewegen keine Zahl, ein geaenderter Phasenname
            // auch nicht. Verglichen wird darum nicht, was sich geaendert hat,
            // sondern was es bewirkt.
            var alt = BilanzMit(vorher).Normal;
            var neu = BilanzMit(Stamm).Normal;

            return alt == neu ? null : new Planaenderung(alt, neu);
        }
    }

    public void PlanhinweisZurKenntnis()
    {
        _vorherigerStand = null;
        Melden();
    }

    private void FrischeStammdaten(Stammdatensatz satz)
    {
        _vorherigerStand = lader.Vorheriger;
        Stamm = satz;
        Melden();
    }

    private bool _hoertAufStammdaten;

    public async Task LadenAsync()
    {
        // Der Lader kann im Hintergrund einen neueren Stand nachliefern — etwa
        // ein Rezept, das seit dem letzten Start dazugekommen ist.
        if (!_hoertAufStammdaten)
        {
            lader.Aufgefrischt += FrischeStammdaten;
            _hoertAufStammdaten = true;
        }

        Stamm = await lader.LadenAsync();

        var profil = api.ProfilAsync();
        var woche = api.WocheAsync();
        await Task.WhenAll(profil, woche);

        Profil = await profil;
        Woche = await woche;
        Geladen = true;
        Melden();
    }

    // ── Rechnung ────────────────────────────────────────

    public PhasenAnzeige AktivePhase() => AktivePhaseIn(Stamm);

    private PhasenAnzeige AktivePhaseIn(Stammdatensatz satz)
        => satz.Training.Phasen.FirstOrDefault(p => p.Id == Profil.PhaseId) ?? satz.Training.Phasen[0];

    public Bilanz Bilanz() => BilanzMit(Stamm);

    /// <summary>
    /// Dieselbe Rechnung, aber gegen einen beliebigen Stand — gebraucht wird das
    /// nur, um alten und neuen Trainingsplan zu vergleichen. Ein zweiter
    /// Rechenweg entstuende dabei nicht: es ist derselbe Rechner.
    /// </summary>
    private Bilanz BilanzMit(Stammdatensatz satz) => rechner.Bilanz(
        new Profil(Profil.GewichtKg, Profil.ZielKg, Profil.GroesseCm, Profil.Alter,
                   Profil.ProteinFaktor, Profil.TempoKgProWoche),
        AktivePhaseIn(satz).AlsPhase(),
        satz.Training.MetWerte);

    public int TagesZiel(string tag)
    {
        var b = Bilanz();
        return tag == Woche.RefeedTag ? b.Refeed : b.Normal;
    }

    public (int Kcal, int Protein) Tagessumme(string tag) => planung.Tagessumme(Woche, tag, Rezepte);

    public Einkaufsliste Einkaufsliste()
        => planung.Einkaufsliste(Woche, Rezepte, Stamm.Rezepte.Abteilungen);

    public double? Schnitt7(int bisIndex) => rechner.Schnitt7(Profil.Verlauf, bisIndex);

    public Rezept? RezeptMit(string id) => Rezepte.FirstOrDefault(r => r.Id == id);

    public IReadOnlyList<PlanEintrag> Geplant(string tag, string mahlzeit)
        => Woche.Plan.TryGetValue(tag, out var mahlzeiten)
           && mahlzeiten.TryGetValue(mahlzeit, out var eintraege)
            ? eintraege
            : [];

    // ── Aendern: Woche ──────────────────────────────────

    /// <summary>
    /// Legt ein Gericht und merkt sich, mit welchen Naehrwerten geplant wurde.
    /// Aendert jemand das Rezept spaeter, faellt das am Tag auf, statt die
    /// Wochenbilanz still zu verschieben.
    /// </summary>
    public void GerichtLegen(string tag, string mahlzeit, string rezeptId)
        => PlanAendern(tag, mahlzeit, eintraege =>
        {
            var rezept = RezeptMit(rezeptId);
            return [.. eintraege, new PlanEintrag(rezeptId, 1, rezept?.Kcal, rezept?.Protein)];
        });

    /// <summary>
    /// Was sich seit dem Planen an einem Gericht geaendert hat — <c>null</c>,
    /// wenn nichts, wenn das Rezept fehlt, oder wenn der Eintrag aus der Zeit
    /// vor den gemerkten Zahlen stammt.
    /// </summary>
    public Abweichung? AbweichungFuer(PlanEintrag eintrag)
        => Abweichung.Zwischen(eintrag, RezeptMit(eintrag.RezeptId));

    /// <summary>Nimmt die neuen Zahlen zur Kenntnis — der Hinweis verschwindet, gerechnet wurde ohnehin damit.</summary>
    public void ZurKenntnis(string tag, string mahlzeit, int stelle)
        => PlanAendern(tag, mahlzeit, eintraege =>
        [
            .. eintraege.Select((e, i) =>
                i == stelle && RezeptMit(e.RezeptId) is { } r
                    ? e with { KcalBeimPlanen = r.Kcal, ProteinBeimPlanen = r.Protein }
                    : e)
        ]);

    public void GerichtEntfernen(string tag, string mahlzeit, int stelle)
        => PlanAendern(tag, mahlzeit, eintraege => [.. eintraege.Where((_, i) => i != stelle)]);

    public void PortionenSetzen(string tag, string mahlzeit, int stelle, int portionen)
        => PlanAendern(tag, mahlzeit, eintraege =>
            [.. eintraege.Select((e, i) => i == stelle ? e with { Portionen = Math.Clamp(portionen, 1, 9) } : e)]);

    /// <summary>
    /// Leert die Gerichte. Die Gaeste bleiben stehen — der Besuch am Wochenende
    /// ist eine Tatsache und haengt nicht daran, was auf dem Tisch steht.
    /// </summary>
    public void WocheLeeren() => WocheAendern(w => w with
    {
        Plan = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>()
    });

    // ── Aendern: Gaeste ─────────────────────────────────

    /// <summary>
    /// Setzt die zusaetzlichen Esser eines Tages. Auf null zurueck heisst: der
    /// Tag ist wieder ein gewoehnlicher — dann fallen auch seine
    /// Mahlzeit-Ausnahmen weg, sonst bliebe unsichtbarer Zustand liegen, der
    /// beim naechsten Besuch ueberraschend wieder auftaucht.
    /// </summary>
    public void GaesteAmTagSetzen(string tag, int zahl) => WocheAendern(w =>
    {
        var wert = Math.Clamp(zahl, 0, 8);

        var amTag = Kopie(w.GaesteTag);
        if (wert > 0) amTag[tag] = wert; else amTag.Remove(tag);

        var ausnahmen = Kopie(w.GaesteMahlzeit);
        if (wert == 0)
        {
            foreach (var mahlzeit in Core.Wochenplanung.Contracts.Woche.Mahlzeiten)
            {
                ausnahmen.Remove(WochenStand.Mahlzeitschluessel(tag, mahlzeit.Schluessel));
            }
        }

        return w with { GaesteTag = amTag, GaesteMahlzeit = ausnahmen };
    });

    /// <summary>Loest eine Mahlzeit vom Tag ab — auch auf 0, das ist der Fall „fruehstuecken nicht mit".</summary>
    public void GaesteAnDerMahlzeitSetzen(string tag, string mahlzeit, int zahl) => WocheAendern(w =>
    {
        var ausnahmen = Kopie(w.GaesteMahlzeit);
        ausnahmen[WochenStand.Mahlzeitschluessel(tag, mahlzeit)] = Math.Clamp(zahl, 0, 8);
        return w with { GaesteMahlzeit = ausnahmen };
    });

    /// <summary>Haengt die Mahlzeit wieder an den Tag.</summary>
    public void GaesteAnDerMahlzeitZuruecksetzen(string tag, string mahlzeit) => WocheAendern(w =>
    {
        var ausnahmen = Kopie(w.GaesteMahlzeit);
        ausnahmen.Remove(WochenStand.Mahlzeitschluessel(tag, mahlzeit));
        return w with { GaesteMahlzeit = ausnahmen };
    });

    /// <summary>Was tatsaechlich gekocht wird: die eigene Portion plus die Gaeste.</summary>
    public int Kochportionen(string tag, string mahlzeit, PlanEintrag eintrag)
        => eintrag.Portionen + Woche.Gaeste(tag, mahlzeit);

    private static Dictionary<string, int> Kopie(IReadOnlyDictionary<string, int>? quelle)
        => quelle is null ? [] : new Dictionary<string, int>(quelle);

    /// <summary>
    /// Was das Fuellen nicht einhalten konnte — leer, solange nicht gefuellt
    /// wurde. Der Rueckfall auf die volle Auswahl ist noetig, damit kein Tag
    /// leer bleibt; unausgesprochen waere er eine regelwidrige Woche ohne Grund.
    /// </summary>
    public IReadOnlyList<string> Fuellhinweise { get; private set; } = [];

    public void WocheFuellen()
    {
        Fuellhinweise = planung.Fuellhinweise(Rezepte);
        WocheAendern(w => planung.AutomatischFuellen(w, Rezepte, Bilanz()));
    }

    public void FuellhinweiseZurKenntnis()
    {
        Fuellhinweise = [];
        Melden();
    }

    public void RefeedTagSetzen(string tag) => WocheAendern(w => w with { RefeedTag = tag });

    public void HakenWoche(string posten, bool gesetzt) => WocheAendern(w => w with
    {
        HakenWoche = Gesetzt(w.HakenWoche, posten, gesetzt)
    });

    public void HakenGrundstock(string posten, bool gesetzt) => WocheAendern(w => w with
    {
        HakenGrundstock = Gesetzt(w.HakenGrundstock, posten, gesetzt)
    });

    public bool IstGehakt(IReadOnlyDictionary<string, bool> haken, string posten)
        => haken.TryGetValue(posten, out var gesetzt) && gesetzt;

    private static Dictionary<string, bool> Gesetzt(
        IReadOnlyDictionary<string, bool> haken, string posten, bool gesetzt)
    {
        var neu = new Dictionary<string, bool>(haken);
        if (gesetzt) neu[posten] = true; else neu.Remove(posten);
        return neu;
    }

    private void PlanAendern(
        string tag, string mahlzeit, Func<IReadOnlyList<PlanEintrag>, IReadOnlyList<PlanEintrag>> aenderung)
        => WocheAendern(w =>
        {
            var plan = w.Plan.ToDictionary(x => x.Key, x => x.Value);
            var mahlzeiten = plan.TryGetValue(tag, out var da)
                ? da.ToDictionary(x => x.Key, x => x.Value)
                : [];

            mahlzeiten[mahlzeit] = aenderung(
                mahlzeiten.TryGetValue(mahlzeit, out var eintraege) ? eintraege : []);
            plan[tag] = mahlzeiten;

            return w with { Plan = plan };
        });

    // ── Aendern: Profil ─────────────────────────────────

    /// <summary>Traegt das Gewicht fuer einen Tag ein — ein zweiter Eintrag am selben Tag ersetzt den ersten.</summary>
    public void GewichtEintragen(DateOnly datum, double kg) => ProfilAendern(p =>
    {
        var verlauf = p.Verlauf.Where(e => e.Datum != datum)
            .Append(new Gewichtseintrag(datum, kg))
            .OrderBy(e => e.Datum)
            .ToList();

        // Der juengste Eintrag ist ab jetzt das Arbeitsgewicht — daran haengen
        // Verbrauchstabelle, Kalorienziel und Einkaufsmengen.
        return p with { Verlauf = verlauf, GewichtKg = verlauf[^1].Kg };
    });

    public void ProfilSetzen(Func<ProfilStand, ProfilStand> aenderung) => ProfilAendern(aenderung);

    // ── Schreiben ───────────────────────────────────────

    private void ProfilAendern(Func<ProfilStand, ProfilStand> aenderung)
    {
        Profil = aenderung(Profil);
        Melden();
        Anstossen(ref _profilWartet, () => api.ProfilSpeichernAsync(Profil));
    }

    private void WocheAendern(Func<WochenStand, WochenStand> aenderung)
    {
        Woche = aenderung(Woche);
        Melden();
        Anstossen(ref _wocheWartet, () => api.WocheSpeichernAsync(Woche));
    }

    /// <summary>Schreibt sofort, was noch aussteht — beim Tabwechsel und beim Verlassen.</summary>
    public async Task JetztSchreibenAsync()
    {
        foreach (var wartet in new[] { _profilWartet, _wocheWartet })
        {
            if (wartet is not null) await wartet.CancelAsync();
        }
        _profilWartet = null;
        _wocheWartet = null;

        await ErneutVersuchenAsync();
    }

    /// <summary>Nach einem Fehler von Hand ausgeloest.</summary>
    public async Task ErneutVersuchenAsync()
    {
        await SchreibenAsync(() => api.ProfilSpeichernAsync(Profil));
        await SchreibenAsync(() => api.WocheSpeichernAsync(Woche));
    }

    private void Anstossen(ref CancellationTokenSource? wartet, Func<Task> schreiben)
    {
        wartet?.Cancel();
        wartet?.Dispose();
        var neu = new CancellationTokenSource();
        wartet = neu;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Ruhefrist, neu.Token);
            }
            catch (TaskCanceledException)
            {
                return;   // eine neue Eingabe kam dazwischen — die spaetere schreibt
            }
            await SchreibenAsync(schreiben);
        });
    }

    private async Task SchreibenAsync(Func<Task> schreiben)
    {
        Lage = Speicherlage.Schreibt;
        Melden();
        try
        {
            await schreiben();
            Lage = Speicherlage.Ruhe;
        }
        catch (Exception)
        {
            // Die Zahl bleibt sichtbar, sie ist nur noch nicht sicher — der
            // Zustandsstreifen sagt das, statt still zu scheitern.
            Lage = Speicherlage.Fehler;
        }
        Melden();
    }

    private void Melden() => Geaendert?.Invoke();
}
