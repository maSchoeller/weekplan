using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Core.Stammdaten;

/// <summary>
/// Setzt die Stammdaten aus ihren Dokumenten zusammen. Der Name weicht vom
/// Muster der anderen Slices ab (dort heisst die Umsetzung wie der Slice), weil
/// <see cref="Stammdatensatz"/> als Typ bereits so heisst.
/// </summary>
internal sealed class Stammdatendienst(IAblage ablage) : IStammdaten
{
    public async Task<Stammdatensatz> AllesAsync(CancellationToken ct = default)
    {
        var abteilungen = ablage.LesenAsync<Abteilungsdaten>(Namen.Liste, Namen.Abteilungen, ct);
        var training = ablage.LesenAsync<Trainingsdaten>(Namen.Liste, Namen.Training, ct);
        var grundstock = ablage.LesenAsync<Grundstockdaten>(Namen.Liste, Namen.Grundstock, ct);
        var rezepte = ablage.AlleAsync<Rezept>(Namen.Rezept, ct);
        await Task.WhenAll(abteilungen, training, grundstock, rezepte);

        // Fehlt eine der drei Listen, ist die Ablage nicht befuellt. Ohne
        // Abteilungen sortiert die Einkaufsliste nicht, ohne Phasen rechnet die
        // App nicht — eine leere Antwort waere also nicht leer, sondern falsch.
        var kopf = await abteilungen ?? throw StammdatenFehlenException.Fuer(Namen.Abteilungen);
        var phasen = await training ?? throw StammdatenFehlenException.Fuer(Namen.Training);
        var vorrat = await grundstock ?? throw StammdatenFehlenException.Fuer(Namen.Grundstock);

        // Cosmos sichert keine Reihenfolge zu. Nach Namen sortiert liest die
        // Uebersicht sich gleich, egal woher die Daten kommen.
        var sortiert = (await rezepte).OrderBy(r => r.Name, StringComparer.Ordinal).ToList();

        return new Stammdatensatz(
            new Rezeptdaten(kopf.Hinweis, kopf.Abteilungen, sortiert), phasen, vorrat);
    }

    public Task<Rezept?> RezeptAsync(string id, CancellationToken ct = default)
        => ablage.LesenAsync<Rezept>(Namen.Rezept, id, ct);

    public async Task BefuellenAsync(Stammdatensatz daten, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(daten);

        await ablage.SchreibenAsync(Namen.Liste, Namen.Abteilungen,
            new Abteilungsdaten(daten.Rezepte.Hinweis, daten.Rezepte.Abteilungen), ct);
        await ablage.SchreibenAsync(Namen.Liste, Namen.Training, daten.Training, ct);
        await ablage.SchreibenAsync(Namen.Liste, Namen.Grundstock, daten.Grundstock, ct);

        foreach (var rezept in daten.Rezepte.Rezepte)
        {
            await ablage.SchreibenAsync(Namen.Rezept, rezept.Id, rezept, ct);
        }
    }

    public async Task<Rezept> AnlegenAsync(Rezeptentwurf entwurf, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entwurf);

        await PruefenAsync(entwurf, ct);
        var id = Rezeptpruefung.KennungAus(entwurf.Name);

        // Nicht upsert: ein zweites Rezept gleichen Namens wuerde das erste
        // stillschweigend ueberschreiben, und niemand saehe es.
        if (await ablage.LesenAsync<Rezept>(Namen.Rezept, id, ct) is not null)
        {
            throw new StammdatenUngueltigException(
                $"Es gibt schon ein Rezept mit der Kennung '{id}'. Zum Ersetzen aendern statt anlegen.");
        }

        return await SchreibenAsync(id, entwurf, ct);
    }

    public async Task<Rezept> AendernAsync(string id, Rezeptentwurf entwurf, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(entwurf);

        await PruefenAsync(entwurf, ct);

        // Aendern legt nicht an: sonst entstuende bei einem Tippfehler in der
        // Kennung ein zweites Rezept statt einer Fehlermeldung.
        if (await ablage.LesenAsync<Rezept>(Namen.Rezept, id, ct) is null)
        {
            throw new StammdatenUngueltigException($"Es gibt kein Rezept mit der Kennung '{id}'.");
        }

        return await SchreibenAsync(id, entwurf, ct);
    }

    public Task<bool> LoeschenAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return ablage.LoeschenAsync(Namen.Rezept, id, ct);
    }

    public async Task<Trainingsdaten> TrainingSchreibenAsync(
        Trainingsentwurf entwurf, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entwurf);
        Planpruefung.Training(entwurf);

        // Die Regeln kommen nicht aus dem Entwurf — der hat gar kein solches
        // Feld. Hier werden die vorhandenen zurueckgelegt; das ist die zweite
        // Haelfte des Schreibschutzes.
        var bisher = await ablage.LesenAsync<Trainingsdaten>(Namen.Liste, Namen.Training, ct)
                     ?? throw StammdatenFehlenException.Fuer(Namen.Training);

        var neu = new Trainingsdaten(
            entwurf.Hinweis, entwurf.MetWerte, entwurf.Phasen, entwurf.Kraftplan, bisher.Regeln);

        await ablage.SchreibenAsync(Namen.Liste, Namen.Training, neu, ct);
        return neu;
    }

    public async Task<Grundstockdaten> GrundstockSchreibenAsync(
        Grundstockdaten daten, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(daten);
        Planpruefung.Grundstock(daten);

        await ablage.SchreibenAsync(Namen.Liste, Namen.Grundstock, daten, ct);
        return daten;
    }

    /// <summary>
    /// Faellt eine Abteilung weg, in der noch Zutaten stehen, wird das nicht
    /// abgelehnt, sondern aufgeraeumt: die Zutaten wandern in die
    /// Sammelabteilung, und die haengt sich ans Ende der Liste. Nichts
    /// verschwindet, kein Rezept wird ungueltig.
    ///
    /// <para>
    /// Der Vorgang ist <b>nicht atomar</b> — bricht er zwischen Liste und
    /// Rezepten ab, stehen beide kurz auseinander. Hingenommen, weil eine Zutat
    /// mit unbekannter Abteilung auf der Einkaufsliste lediglich unten landet.
    /// Steht so in <c>debt.md</c>.
    /// </para>
    /// </summary>
    public async Task<Abteilungsumzug> AbteilungenSchreibenAsync(
        Abteilungsentwurf entwurf, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entwurf);
        Planpruefung.Abteilungen(entwurf);

        var erlaubt = entwurf.Abteilungen.ToHashSet(StringComparer.Ordinal);
        var alle = await ablage.AlleAsync<Rezept>(Namen.Rezept, ct);

        var umgezogen = alle
            .Select(r => (Alt: r, Neu: NachSonstiges(r, erlaubt)))
            .Where(x => x.Neu is not null)
            .ToList();

        var zutaten = umgezogen.Sum(x => x.Alt.Zutaten.Count(z => !erlaubt.Contains(z.Abt)));

        var liste = umgezogen.Count > 0 && !erlaubt.Contains(Namen.Sammelabteilung)
            ? (IReadOnlyList<string>)[.. entwurf.Abteilungen, Namen.Sammelabteilung]
            : entwurf.Abteilungen;

        var kopf = new Abteilungsdaten(entwurf.Hinweis, liste);
        await ablage.SchreibenAsync(Namen.Liste, Namen.Abteilungen, kopf, ct);

        foreach (var (_, neu) in umgezogen)
        {
            await ablage.SchreibenAsync(Namen.Rezept, neu!.Id, neu, ct);
        }

        return new Abteilungsumzug(kopf, zutaten, umgezogen.Count);
    }

    /// <returns><c>null</c>, wenn das Rezept gar nicht betroffen ist.</returns>
    private static Rezept? NachSonstiges(Rezept rezept, HashSet<string> erlaubt)
    {
        if (rezept.Zutaten.All(z => erlaubt.Contains(z.Abt))) return null;

        return rezept with
        {
            Zutaten = [.. rezept.Zutaten.Select(z =>
                erlaubt.Contains(z.Abt) ? z : z with { Abt = Namen.Sammelabteilung })]
        };
    }

    private async Task PruefenAsync(Rezeptentwurf entwurf, CancellationToken ct)
    {
        var kopf = await ablage.LesenAsync<Abteilungsdaten>(Namen.Liste, Namen.Abteilungen, ct)
                   ?? throw StammdatenFehlenException.Fuer(Namen.Abteilungen);

        Rezeptpruefung.Pruefen(entwurf, kopf.Abteilungen);
    }

    private async Task<Rezept> SchreibenAsync(string id, Rezeptentwurf entwurf, CancellationToken ct)
    {
        var rezept = new Rezept(
            id, entwurf.Name.Trim(), entwurf.Kategorie, entwurf.ZeitMin, entwurf.Kalt, entwurf.Prep,
            entwurf.Kcal, entwurf.Protein, entwurf.Zutaten, entwurf.Anleitung);

        await ablage.SchreibenAsync(Namen.Rezept, id, rezept, ct);
        return rezept;
    }
}
