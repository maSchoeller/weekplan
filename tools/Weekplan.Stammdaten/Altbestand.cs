using System.Text.Json;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Stammdaten;

/// <summary>
/// Die drei JSON-Dateien, mit denen weekplan angefangen hat, und ihr Weg in das
/// heutige Modell. Sie liegen eingefroren unter <c>altbestand/</c> — sie sind
/// Umzugsgrundlage, keine laufende Datenquelle. Nach dem Umzug aendert sich
/// nichts mehr an ihnen.
/// </summary>
public static class Altbestand
{
    private static readonly JsonSerializerOptions Format = new(JsonSerializerDefaults.Web);

    /// <summary>Das alte Rezept: Zubereitung war eine Liste kurzer Saetze.</summary>
    public sealed record AltRezept(
        string Id,
        string Name,
        string Kategorie,
        int ZeitMin,
        bool Kalt,
        int Kcal,
        int Protein,
        IReadOnlyList<Zutat> Zutaten,
        IReadOnlyList<string> Schritte);

    public sealed record AltRezeptdaten(
        string Hinweis,
        IReadOnlyList<string> Abteilungen,
        IReadOnlyList<AltRezept> Rezepte);

    public static async Task<Stammdatensatz> LesenAsync(string ordner, CancellationToken ct = default)
    {
        var rezepte = await DateiAsync<AltRezeptdaten>(ordner, "rezepte.json", ct);
        var training = await DateiAsync<Trainingsdaten>(ordner, "training.json", ct);
        var grundstock = await DateiAsync<Grundstockdaten>(ordner, "grundstock.json", ct);

        return new Stammdatensatz(
            new Rezeptdaten(rezepte.Hinweis, rezepte.Abteilungen, [.. rezepte.Rezepte.Select(Umwandeln)]),
            training,
            grundstock);
    }

    /// <summary>
    /// Aus den Einzelschritten wird eine nummerierte Markdown-Liste. Das ist die
    /// verlustfreie Uebersetzung: dieselben Saetze, dieselbe Reihenfolge,
    /// dieselbe Nummerierung, nur ab jetzt in einem Feld, das auch
    /// Zwischenueberschriften und Fliesstext tragen kann.
    /// </summary>
    public static Rezept Umwandeln(AltRezept alt) => new(
        alt.Id, alt.Name, alt.Kategorie, alt.ZeitMin, alt.Kalt, alt.Kcal, alt.Protein,
        alt.Zutaten,
        string.Join("\n", alt.Schritte.Select((schritt, i) => $"{i + 1}. {schritt}")));

    private static async Task<T> DateiAsync<T>(string ordner, string name, CancellationToken ct)
    {
        var pfad = Path.Combine(ordner, name);
        if (!File.Exists(pfad)) throw new FileNotFoundException($"Der Altbestand fehlt: {pfad}", pfad);

        await using var strom = File.OpenRead(pfad);
        return await JsonSerializer.DeserializeAsync<T>(strom, Format, ct)
               ?? throw new InvalidOperationException($"{name} ist leer.");
    }
}
