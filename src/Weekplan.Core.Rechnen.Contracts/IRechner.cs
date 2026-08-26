namespace Weekplan.Core.Rechnen.Contracts;

/// <summary>
/// Die gesamte Rechnung von weekplan: rein, ohne Speicher und ohne Uhr.
/// Formeln und Begruendungen stehen in docs/plan.md.
/// </summary>
public interface IRechner
{
    /// <summary>Grundumsatz nach Mifflin-St Jeor, kcal pro Tag.</summary>
    double Grundumsatz(Profil profil);

    /// <summary>Alltagsumsatz ohne geplanten Sport (Buerojob, Faktor 1,28).</summary>
    double Alltagsumsatz(Profil profil);

    /// <summary>Netto-Kalorien einer Einheit; der Grundumsatz waehrend der Einheit ist abgezogen.</summary>
    double EinheitKcal(double met, int minuten, double gewichtKg);

    /// <summary>Verbrauch je Trainingstag und Wochensumme einer Phase.</summary>
    Sportwoche PhaseSport(Phase? phase, IReadOnlyDictionary<string, MetWert> metWerte, double gewichtKg);

    /// <summary>Die vollstaendige Rechnung fuer einen Profilstand.</summary>
    Bilanz Bilanz(Profil profil, Phase? phase, IReadOnlyDictionary<string, MetWert> metWerte);

    /// <summary>Mittel der letzten sieben Eintraege bis einschliesslich <paramref name="bisIndex"/>.</summary>
    double? Schnitt7(IReadOnlyList<Gewichtseintrag> verlauf, int bisIndex);
}
