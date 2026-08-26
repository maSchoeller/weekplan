using Weekplan.Core.Rechnen.Contracts;

namespace Weekplan.Core.Rechnen;

/// <summary>
/// Die Rechnung aus docs/plan.md, eins zu eins wie in der bisherigen App.
/// Rein: keine Persistenz, keine Uhr, keine Oberflaeche.
/// </summary>
internal sealed class Rechner : IRechner
{
    /// <summary>Energiegehalt von 1 kg Koerperfett — das Bindeglied zwischen Defizit und Tempo.</summary>
    private const double KcalProKg = 7700;

    /// <summary>Buerojob mit wenig Bewegung. Geplanter Sport steckt bewusst nicht darin.</summary>
    private const double Alltagsfaktor = 1.28;

    // JavaScript rundet die Haelfte vom Nullpunkt weg, .NET standardmaessig zur
    // geraden Zahl. Ohne diese Angabe weicht jede zweite Zahl von der alten App ab.
    private static int Runde(double wert) => (int)Math.Round(wert, MidpointRounding.AwayFromZero);

    public double Grundumsatz(Profil profil)
        => 10 * profil.GewichtKg + 6.25 * profil.GroesseCm - 5 * profil.Alter + 5;

    public double Alltagsumsatz(Profil profil) => Grundumsatz(profil) * Alltagsfaktor;

    public double EinheitKcal(double met, int minuten, double gewichtKg)
        => (met - 1) * 1.05 * gewichtKg * (minuten / 60.0);

    public Sportwoche PhaseSport(Phase? phase, IReadOnlyDictionary<string, MetWert> metWerte, double gewichtKg)
    {
        if (phase is null) return new Sportwoche([], 0);

        var proTag = phase.Tage.Select(tag =>
        {
            var kcal = tag.Einheiten.Sum(einheit => metWerte.TryGetValue(einheit.Typ, out var met)
                ? EinheitKcal(met.Met, einheit.Minuten, gewichtKg)
                : 0);
            return new TagesVerbrauch(tag.Tag, tag.Ort, tag.Einheiten, Runde(kcal));
        }).ToList();

        return new Sportwoche(proTag, proTag.Sum(t => t.Kcal));
    }

    public Bilanz Bilanz(Profil profil, Phase? phase, IReadOnlyDictionary<string, MetWert> metWerte)
    {
        var sport = PhaseSport(phase, metWerte, profil.GewichtKg);
        var sportSchnitt = sport.Woche / 7.0;
        var gesamt = Alltagsumsatz(profil) + sportSchnitt;

        // Normalerweise gibt die Phase das Defizit vor und das Tempo folgt daraus.
        // Ist ein eigenes Tempo gesetzt, dreht sich die Rechnung um.
        var phasenDefizit = phase?.DefizitZiel ?? 0;
        var eigenesTempo = profil.TempoKgProWoche is > 0;
        var defizit = eigenesTempo
            ? Runde(profil.TempoKgProWoche!.Value * KcalProKg / 7)
            : phasenDefizit;

        // Der Refeed-Tag laeuft ohne Defizit. Damit die Wochenbilanz trotzdem
        // 7 × Tagesdefizit ergibt, tragen die uebrigen sechs Tage je ein Siebtel mehr.
        var defizit6 = defizit * 7 / 6.0;

        return new Bilanz(
            Grundumsatz: Runde(Grundumsatz(profil)),
            Alltag: Runde(Alltagsumsatz(profil)),
            SportSchnitt: Runde(sportSchnitt),
            Gesamt: Runde(gesamt),
            Defizit: defizit,
            PhasenDefizit: phasenDefizit,
            EigenesTempo: eigenesTempo,
            Normal: Runde(gesamt - defizit6),
            Refeed: Runde(gesamt),
            Protein: Runde(profil.ProteinFaktor * profil.ZielKg),
            Wochendefizit: Runde(defizit * 7),
            KgProWoche: defizit * 7 / KcalProKg,
            PhasenTempo: phasenDefizit * 7 / KcalProKg,
            Sport: sport);
    }

    public double? Schnitt7(IReadOnlyList<Gewichtseintrag> verlauf, int bisIndex)
    {
        if (bisIndex < 0 || verlauf.Count == 0) return null;

        var von = Math.Max(0, bisIndex - 6);
        var anzahl = Math.Min(bisIndex, verlauf.Count - 1) - von + 1;
        if (anzahl <= 0) return null;

        return verlauf.Skip(von).Take(anzahl).Average(e => e.Kg);
    }
}
