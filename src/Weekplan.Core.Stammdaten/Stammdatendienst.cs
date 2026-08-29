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
        var id = Pruefung.KennungAus(entwurf.Name);

        // Nicht upsert: ein zweites Rezept gleichen Namens wuerde das erste
        // stillschweigend ueberschreiben, und niemand saehe es.
        if (await ablage.LesenAsync<Rezept>(Namen.Rezept, id, ct) is not null)
        {
            throw new RezeptUngueltigException(
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
            throw new RezeptUngueltigException($"Es gibt kein Rezept mit der Kennung '{id}'.");
        }

        return await SchreibenAsync(id, entwurf, ct);
    }

    public Task<bool> LoeschenAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return ablage.LoeschenAsync(Namen.Rezept, id, ct);
    }

    private async Task PruefenAsync(Rezeptentwurf entwurf, CancellationToken ct)
    {
        var kopf = await ablage.LesenAsync<Abteilungsdaten>(Namen.Liste, Namen.Abteilungen, ct)
                   ?? throw StammdatenFehlenException.Fuer(Namen.Abteilungen);

        Pruefung.Pruefen(entwurf, kopf.Abteilungen);
    }

    private async Task<Rezept> SchreibenAsync(string id, Rezeptentwurf entwurf, CancellationToken ct)
    {
        var rezept = new Rezept(
            id, entwurf.Name.Trim(), entwurf.Kategorie, entwurf.ZeitMin, entwurf.Kalt,
            entwurf.Kcal, entwurf.Protein, entwurf.Zutaten, entwurf.Anleitung);

        await ablage.SchreibenAsync(Namen.Rezept, id, rezept, ct);
        return rezept;
    }
}
