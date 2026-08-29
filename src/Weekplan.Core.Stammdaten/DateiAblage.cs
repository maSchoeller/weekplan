using System.Text.Json;

namespace Weekplan.Core.Stammdaten;

/// <summary>
/// Dokumente als JSON-Dateien: <c>&lt;wurzel&gt;/&lt;art&gt;/&lt;id&gt;.json</c>.
/// Fuer die lokale Entwicklung, den Smoketest und die schnellen Tests; in Azure
/// tritt Cosmos an ihre Stelle.
/// </summary>
internal sealed class DateiAblage(string wurzel) : IAblage
{
    private static readonly JsonSerializerOptions Format = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<T?> LesenAsync<T>(string art, string id, CancellationToken ct) where T : class
    {
        var pfad = Pfad(art, id);
        if (!File.Exists(pfad)) return null;

        await using var strom = File.OpenRead(pfad);
        return await JsonSerializer.DeserializeAsync<T>(strom, Format, ct);
    }

    public async Task<IReadOnlyList<T>> AlleAsync<T>(string art, CancellationToken ct) where T : class
    {
        var ordner = Path.Combine(wurzel, Sicher(art));
        if (!Directory.Exists(ordner)) return [];

        var alle = new List<T>();
        foreach (var pfad in Directory.EnumerateFiles(ordner, "*.json"))
        {
            await using var strom = File.OpenRead(pfad);
            if (await JsonSerializer.DeserializeAsync<T>(strom, Format, ct) is { } inhalt) alle.Add(inhalt);
        }
        return alle;
    }

    public async Task SchreibenAsync<T>(string art, string id, T inhalt, CancellationToken ct) where T : class
    {
        var pfad = Pfad(art, id);
        Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);

        // Erst daneben schreiben, dann umbenennen: ein Absturz mitten im
        // Schreiben hinterlaesst sonst eine halbe Datei, und die ist schlimmer
        // als eine alte.
        var vorlaeufig = pfad + ".neu";
        await using (var strom = File.Create(vorlaeufig))
        {
            await JsonSerializer.SerializeAsync(strom, inhalt, Format, ct);
        }
        File.Move(vorlaeufig, pfad, overwrite: true);
    }

    public Task<bool> LoeschenAsync(string art, string id, CancellationToken ct)
    {
        var pfad = Pfad(art, id);
        if (!File.Exists(pfad)) return Task.FromResult(false);

        File.Delete(pfad);
        return Task.FromResult(true);
    }

    private string Pfad(string art, string id)
        => Path.Combine(wurzel, Sicher(art), Sicher(id) + ".json");

    // Eine Rezeptkennung kommt aus einer Eingabe. Ohne diese Pruefung waere ein
    // Name mit Pfadtrennern ein Zugriff ausserhalb des Ordners.
    private static string Sicher(string teil)
    {
        if (string.IsNullOrWhiteSpace(teil)
            || teil.Contains(Path.DirectorySeparatorChar)
            || teil.Contains(Path.AltDirectorySeparatorChar)
            || teil.Contains("..") || teil.Contains(':')
            || teil.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"Unzulaessiger Name: {teil}", nameof(teil));
        }
        return teil;
    }
}
