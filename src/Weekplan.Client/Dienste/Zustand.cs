using Weekplan.Client.Daten;
using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Tagebuch.Contracts;
using Weekplan.Core.Wochenplanung.Contracts;
using Profil = Weekplan.Core.Rechnen.Contracts.Profil;

namespace Weekplan.Client.Dienste;

public enum Speicherlage { Ruhe, Schreibt, Fehler }

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

    public Stammdaten Stamm { get; private set; } = null!;
    public ProfilStand Profil { get; private set; } = ProfilStand.Leer;
    public WochenStand Woche { get; private set; } = WochenStand.Leer;

    public Speicherlage Lage { get; private set; } = Speicherlage.Ruhe;
    public bool Geladen { get; private set; }

    /// <summary>Meldet jede Aenderung, damit die Oberflaeche neu zeichnet.</summary>
    public event Action? Geaendert;

    public IReadOnlyList<Rezept> Rezepte => Stamm.Rezepte.Rezepte;

    public async Task LadenAsync()
    {
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

    public PhasenAnzeige AktivePhase()
        => Stamm.Training.Phasen.FirstOrDefault(p => p.Id == Profil.PhaseId) ?? Stamm.Training.Phasen[0];

    public Bilanz Bilanz() => rechner.Bilanz(
        new Profil(Profil.GewichtKg, Profil.ZielKg, Profil.GroesseCm, Profil.Alter,
                   Profil.ProteinFaktor, Profil.TempoKgProWoche),
        AktivePhase().AlsPhase(),
        Stamm.Training.MetWerte);

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

    public void GerichtLegen(string tag, string mahlzeit, string rezeptId)
        => PlanAendern(tag, mahlzeit, eintraege => [.. eintraege, new PlanEintrag(rezeptId, 1)]);

    public void GerichtEntfernen(string tag, string mahlzeit, int stelle)
        => PlanAendern(tag, mahlzeit, eintraege => [.. eintraege.Where((_, i) => i != stelle)]);

    public void PortionenSetzen(string tag, string mahlzeit, int stelle, int portionen)
        => PlanAendern(tag, mahlzeit, eintraege =>
            [.. eintraege.Select((e, i) => i == stelle ? e with { Portionen = Math.Clamp(portionen, 1, 9) } : e)]);

    public void WocheLeeren() => WocheAendern(w => w with
    {
        Plan = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>()
    });

    public void WocheFuellen() => WocheAendern(w => planung.AutomatischFuellen(w, Rezepte, Bilanz()));

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
