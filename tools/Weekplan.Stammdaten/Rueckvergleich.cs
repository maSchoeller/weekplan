using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Stammdaten;

/// <summary>
/// Nach dem Schreiben wird gelesen und verglichen. Ein Umzug, der nur meldet
/// „fertig", beweist nichts — geprueft wird Feld fuer Feld, gegen das, was
/// wirklich in der Ablage steht.
/// </summary>
public static class Rueckvergleich
{
    public static IReadOnlyList<string> Vergleichen(Stammdatensatz erwartet, Stammdatensatz gelesen)
    {
        var klagen = new List<string>();

        if (erwartet.Rezepte.Hinweis != gelesen.Rezepte.Hinweis) klagen.Add("Der Hinweis der Rezepte weicht ab.");
        if (!erwartet.Rezepte.Abteilungen.SequenceEqual(gelesen.Rezepte.Abteilungen))
        {
            klagen.Add("Die Abteilungen weichen ab oder stehen in anderer Reihenfolge.");
        }

        var da = gelesen.Rezepte.Rezepte.ToDictionary(r => r.Id);
        foreach (var soll in erwartet.Rezepte.Rezepte)
        {
            if (!da.TryGetValue(soll.Id, out var ist))
            {
                klagen.Add($"{soll.Id}: fehlt in der Ablage.");
                continue;
            }
            klagen.AddRange(Rezept(soll, ist));
        }

        // Records mit Sammlungen vergleichen ihr Innerstes per Referenz — ein
        // schlichtes == waere hier immer ungleich. Darum Feld fuer Feld.
        klagen.AddRange(Training(erwartet.Training, gelesen.Training));
        klagen.AddRange(Grundstock(erwartet.Grundstock, gelesen.Grundstock));

        return klagen;
    }

    private static IEnumerable<string> Rezept(Rezept soll, Rezept ist)
    {
        if (soll.Name != ist.Name) yield return $"{soll.Id}: Name {soll.Name} statt {ist.Name}.";
        if (soll.Kategorie != ist.Kategorie) yield return $"{soll.Id}: Kategorie weicht ab.";
        if (soll.ZeitMin != ist.ZeitMin) yield return $"{soll.Id}: Zeit weicht ab.";
        if (soll.Kalt != ist.Kalt) yield return $"{soll.Id}: Kennzeichen kalt weicht ab.";
        if (soll.Kcal != ist.Kcal) yield return $"{soll.Id}: kcal {soll.Kcal} statt {ist.Kcal}.";
        if (soll.Protein != ist.Protein) yield return $"{soll.Id}: Protein {soll.Protein} statt {ist.Protein}.";
        if (soll.Anleitung != ist.Anleitung) yield return $"{soll.Id}: die Anleitung weicht ab.";

        if (soll.Zutaten.Count != ist.Zutaten.Count)
        {
            yield return $"{soll.Id}: {ist.Zutaten.Count} Zutaten statt {soll.Zutaten.Count}.";
            yield break;
        }

        // Reihenfolge zaehlt mit: sie ist die Reihenfolge, in der die Zutaten
        // auf der Kochseite stehen.
        for (var i = 0; i < soll.Zutaten.Count; i++)
        {
            if (soll.Zutaten[i] != ist.Zutaten[i])
            {
                yield return $"{soll.Id}: Zutat {i + 1} weicht ab ({soll.Zutaten[i].Name}).";
            }
        }
    }

    private static IEnumerable<string> Training(Trainingsdaten soll, Trainingsdaten ist)
    {
        if (soll.Hinweis != ist.Hinweis) yield return "Training: der Hinweis weicht ab.";
        if (soll.MetWerte.Count != ist.MetWerte.Count) yield return "Training: andere Zahl von MET-Werten.";

        foreach (var (schluessel, wert) in soll.MetWerte)
        {
            if (!ist.MetWerte.TryGetValue(schluessel, out var gelesen) || gelesen != wert)
            {
                yield return $"Training: MET-Wert {schluessel} weicht ab.";
            }
        }

        if (soll.Phasen.Count != ist.Phasen.Count) yield return "Training: andere Zahl von Phasen.";
        if (soll.Regeln.Count != ist.Regeln.Count) yield return "Training: andere Zahl von Regeln.";
        if (soll.Kraftplan.Einheiten.Count != ist.Kraftplan.Einheiten.Count)
        {
            yield return "Training: andere Zahl von Krafteinheiten.";
        }
    }

    private static IEnumerable<string> Grundstock(Grundstockdaten soll, Grundstockdaten ist)
    {
        if (soll.Hinweis != ist.Hinweis) yield return "Grundstock: der Hinweis weicht ab.";
        if (soll.Gruppen.Count != ist.Gruppen.Count)
        {
            yield return "Grundstock: andere Zahl von Gruppen.";
            yield break;
        }

        for (var i = 0; i < soll.Gruppen.Count; i++)
        {
            if (soll.Gruppen[i].Name != ist.Gruppen[i].Name)
            {
                yield return $"Grundstock: Gruppe {i + 1} heisst anders.";
            }
            else if (!soll.Gruppen[i].Artikel.SequenceEqual(ist.Gruppen[i].Artikel))
            {
                yield return $"Grundstock: die Artikel von {soll.Gruppen[i].Name} weichen ab.";
            }
        }
    }
}
