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
}
