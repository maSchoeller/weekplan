using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Core.Stammdaten;

/// <summary>
/// Was Trainingsplan, Grundstock und Abteilungen erfuellen muessen, bevor sie in
/// die Datenbank duerfen. Getrennt von <see cref="Rezeptpruefung"/>, weil eine
/// Datei mit vier Validierern nicht mehr zu ueberblicken waere.
///
/// <para>
/// Die Absage zaehlt wie dort jeden Verstoss auf, statt beim ersten abzubrechen.
/// </para>
/// </summary>
internal static class Planpruefung
{
    public static void Training(Trainingsentwurf entwurf)
    {
        var klagen = new List<string>();

        if (string.IsNullOrWhiteSpace(entwurf.Hinweis)) klagen.Add("Der Hinweistext fehlt.");

        klagen.AddRange(MetWerte(entwurf.MetWerte));
        klagen.AddRange(Phasen(entwurf.Phasen, entwurf.MetWerte));

        if (entwurf.Kraftplan is null) klagen.Add("Der Kraftplan fehlt.");
        else if (string.IsNullOrWhiteSpace(entwurf.Kraftplan.Prinzip))
        {
            klagen.Add("Das Prinzip des Kraftplans fehlt.");
        }

        if (klagen.Count > 0) throw new StammdatenUngueltigException(klagen);
    }

    /// <summary>
    /// Die Rechnung in <c>docs/plan.md</c> §1 lautet
    /// <c>(MET − 1) × 1,05 × kg × min/60</c>. Ein Wert unter 1 macht daraus einen
    /// <b>negativen</b> Verbrauch: der Gesamtumsatz sinkt, die Zielaufnahme faellt,
    /// und nichts davon sieht nach einem Fehler aus. Genau diese Art stiller
    /// Verschiebung soll hier scheitern.
    /// </summary>
    private static IEnumerable<string> MetWerte(IReadOnlyDictionary<string, MetWert>? werte)
    {
        if (werte is null or { Count: 0 })
        {
            yield return "Ohne MET-Werte laesst sich kein Sportverbrauch rechnen.";
            yield break;
        }

        foreach (var (schluessel, wert) in werte)
        {
            if (string.IsNullOrWhiteSpace(wert.Label))
            {
                yield return $"Der MET-Wert '{schluessel}' hat keine Bezeichnung.";
            }

            if (wert.Met < 1)
            {
                yield return $"Der MET-Wert '{schluessel}' ist {wert.Met} und damit kleiner als 1. "
                             + "Die Rechnung (MET − 1) ergaebe einen negativen Verbrauch, der die "
                             + "Zielaufnahme still senken wuerde. Erlaubt sind Werte ab 1.";
            }
        }
    }

    private static IEnumerable<string> Phasen(
        IReadOnlyList<PhasenAnzeige>? phasen, IReadOnlyDictionary<string, MetWert>? werte)
    {
        if (phasen is null or { Count: 0 })
        {
            yield return "Ohne Phasen rechnet die App kein Tagesziel. Mindestens eine wird gebraucht.";
            yield break;
        }

        var bekannt = werte?.Keys.ToList() ?? [];

        foreach (var phase in phasen)
        {
            if (string.IsNullOrWhiteSpace(phase.Id)) yield return "Eine Phase hat keine Kennung.";
            if (string.IsNullOrWhiteSpace(phase.Name)) yield return $"Die Phase '{phase.Id}' hat keinen Namen.";

            if (phase.DefizitZiel < 0)
            {
                yield return $"Die Phase '{phase.Id}' hat ein negatives Defizitziel ({phase.DefizitZiel}).";
            }

            foreach (var tag in phase.Tage ?? [])
            {
                foreach (var einheit in tag.Einheiten ?? [])
                {
                    if (!bekannt.Contains(einheit.Typ))
                    {
                        yield return $"Die Einheit '{einheit.Typ}' am {tag.Tag} der Phase '{phase.Id}' "
                                     + $"nennt einen MET-Typ, den es nicht gibt. "
                                     + $"Erlaubt sind: {string.Join(", ", bekannt)}.";
                    }

                    if (einheit.Min <= 0)
                    {
                        yield return $"Die Einheit '{einheit.Typ}' am {tag.Tag} der Phase '{phase.Id}' "
                                     + "dauert null Minuten.";
                    }
                }
            }
        }
    }

    public static void Grundstock(Grundstockdaten daten)
    {
        var klagen = new List<string>();

        if (string.IsNullOrWhiteSpace(daten.Hinweis)) klagen.Add("Der Hinweistext fehlt.");

        foreach (var gruppe in daten.Gruppen ?? [])
        {
            if (string.IsNullOrWhiteSpace(gruppe.Name)) klagen.Add("Eine Gruppe hat keinen Namen.");

            foreach (var artikel in gruppe.Artikel ?? [])
            {
                if (string.IsNullOrWhiteSpace(artikel.Name))
                {
                    klagen.Add($"Ein Artikel der Gruppe '{gruppe.Name}' hat keinen Namen.");
                }
                else if (string.IsNullOrWhiteSpace(artikel.Menge))
                {
                    // Ohne Menge stuende man im Laden und wuesste nicht, wie viel —
                    // dieselbe Ueberlegung wie bei einer Zutat ohne Gramm.
                    klagen.Add($"Der Artikel '{artikel.Name}' hat keine Menge.");
                }
            }
        }

        if (klagen.Count > 0) throw new StammdatenUngueltigException(klagen);
    }

    public static void Abteilungen(Abteilungsentwurf entwurf)
    {
        var klagen = new List<string>();

        if (string.IsNullOrWhiteSpace(entwurf.Hinweis)) klagen.Add("Der Hinweistext fehlt.");

        if (entwurf.Abteilungen is null or { Count: 0 })
        {
            klagen.Add("Ohne Abteilungen sortiert die Einkaufsliste nicht. Mindestens eine wird gebraucht.");
        }
        else
        {
            if (entwurf.Abteilungen.Any(string.IsNullOrWhiteSpace))
            {
                klagen.Add("Eine Abteilung ohne Namen gibt es nicht.");
            }

            var doppelt = entwurf.Abteilungen
                .GroupBy(a => a, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (doppelt.Count > 0)
            {
                // Zwei gleiche Abteilungen hiessen zwei Stellen im Laden fuer
                // dieselbe Ware — die Einkaufsliste sortierte willkuerlich in eine.
                klagen.Add($"Diese Abteilungen stehen doppelt in der Liste: {string.Join(", ", doppelt)}.");
            }
        }

        if (klagen.Count > 0) throw new StammdatenUngueltigException(klagen);
    }
}
