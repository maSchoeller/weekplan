using System.Globalization;
using System.Text;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Core.Stammdaten;

/// <summary>
/// Was ein Rezept erfuellen muss, bevor es in die Datenbank darf. Eine Stelle,
/// alle Regeln — und die Absage zaehlt jeden Verstoss auf, statt beim ersten
/// abzubrechen: der Aufrufer korrigiert dann einmal statt fuenfmal.
/// </summary>
internal static class Pruefung
{
    private const int AnleitungHoechstens = 20_000;

    public static void Pruefen(Rezeptentwurf entwurf, IReadOnlyList<string> abteilungen)
    {
        var klagen = new List<string>();

        if (string.IsNullOrWhiteSpace(entwurf.Name)) klagen.Add("Der Name fehlt.");
        if (string.IsNullOrWhiteSpace(entwurf.Anleitung)) klagen.Add("Die Anleitung fehlt.");

        if (entwurf.Anleitung?.Length > AnleitungHoechstens)
        {
            klagen.Add($"Die Anleitung ist mit {entwurf.Anleitung.Length} Zeichen zu lang; "
                       + $"erlaubt sind {AnleitungHoechstens}.");
        }

        if (!Kategorien.Erlaubt.Contains(entwurf.Kategorie))
        {
            klagen.Add($"Die Kategorie '{entwurf.Kategorie}' gibt es nicht. "
                       + $"Erlaubt sind: {string.Join(", ", Kategorien.Erlaubt)}.");
        }

        if (entwurf.Kcal <= 0) klagen.Add("kcal muss groesser als null sein.");
        if (entwurf.Protein <= 0) klagen.Add("Protein muss groesser als null sein.");
        if (entwurf.ZeitMin <= 0) klagen.Add("Die Zeit in Minuten muss groesser als null sein.");

        if (entwurf.Zutaten is null or { Count: 0 })
        {
            klagen.Add("Ein Rezept ohne Zutaten ergibt keine Einkaufsliste.");
        }
        else
        {
            foreach (var zutat in entwurf.Zutaten) klagen.AddRange(Zutat(zutat, abteilungen));
        }

        if (klagen.Count > 0) throw new RezeptUngueltigException(klagen);
    }

    private static IEnumerable<string> Zutat(Zutat zutat, IReadOnlyList<string> abteilungen)
    {
        if (string.IsNullOrWhiteSpace(zutat.Name))
        {
            yield return "Eine Zutat hat keinen Namen.";
            yield break;
        }

        if (!abteilungen.Contains(zutat.Abt))
        {
            yield return $"'{zutat.Name}' nennt die Abteilung '{zutat.Abt}', die es nicht gibt. "
                         + $"Erlaubt sind: {string.Join(", ", abteilungen)}.";
        }

        // Eine Zutat ohne Menge waere auf der Einkaufsliste ein Posten ueber
        // nichts — man stuende im Laden und wuesste nicht, wie viel.
        if (zutat.G <= 0 && zutat.Stk <= 0)
        {
            yield return $"'{zutat.Name}' hat weder Gramm noch Stueck.";
        }

        if (zutat.G < 0 || zutat.Stk < 0) yield return $"'{zutat.Name}' hat eine negative Menge.";
    }

    /// <summary>
    /// Die Kennung aus dem Namen: klein, Umlaute aufgeloest, alles Uebrige zu
    /// Bindestrichen. So bleibt sie lesbar — man erkennt ein Rezept in einem
    /// gespeicherten Wochenplan wieder, auch wenn es geloescht wurde.
    /// </summary>
    public static string KennungAus(string name)
    {
        var aufgeloest = new StringBuilder(name.Length);
        foreach (var zeichen in name.ToLowerInvariant())
        {
            aufgeloest.Append(zeichen switch
            {
                'ä' => "ae",
                'ö' => "oe",
                'ü' => "ue",
                'ß' => "ss",
                _ => zeichen.ToString()
            });
        }

        // Was danach noch an Akzenten uebrig ist (é, ñ), faellt hier auf seinen
        // Grundbuchstaben zurueck.
        var zerlegt = aufgeloest.ToString().Normalize(NormalizationForm.FormD);

        var kennung = new StringBuilder(zerlegt.Length);
        foreach (var zeichen in zerlegt)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(zeichen) is UnicodeCategory.NonSpacingMark) continue;

            if (zeichen is >= 'a' and <= 'z' or >= '0' and <= '9') kennung.Append(zeichen);
            else if (kennung.Length > 0 && kennung[^1] != '-') kennung.Append('-');
        }

        var fertig = kennung.ToString().Trim('-');

        if (fertig.Length == 0)
        {
            throw new RezeptUngueltigException(
                $"Aus dem Namen '{name}' laesst sich keine Kennung bilden — er braucht Buchstaben oder Ziffern.");
        }

        return fertig;
    }
}
