using System.Text.Json;

namespace Weekplan.Core.Tagebuch;

/// <summary>
/// Dokumente als JSON-Dateien unter einem Ordner: <c>&lt;wurzel&gt;/&lt;nutzer&gt;/&lt;name&gt;.json</c>.
/// Fuer die lokale Entwicklung und den Smoketest; in Azure tritt Cosmos an ihre Stelle.
/// </summary>
internal sealed class DateiAblage(string wurzel) : IAblage
{
    private static readonly JsonSerializerOptions Format = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<T?> LesenAsync<T>(string nutzerId, string name, CancellationToken ct) where T : class
    {
        var pfad = Pfad(nutzerId, name);
        if (!File.Exists(pfad)) return null;

        await using var strom = File.OpenRead(pfad);
        return await JsonSerializer.DeserializeAsync<T>(strom, Format, ct);
    }

    public async Task SchreibenAsync<T>(string nutzerId, string name, T inhalt, CancellationToken ct) where T : class
    {
        var pfad = Pfad(nutzerId, name);
        Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);

        // Erst daneben schreiben, dann umbenennen: ein Absturz mitten im Schreiben
        // hinterlaesst sonst eine halbe Datei, und die ist schlimmer als eine alte.
        var vorlaeufig = pfad + ".neu";
        await using (var strom = File.Create(vorlaeufig))
        {
            await JsonSerializer.SerializeAsync(strom, inhalt, Format, ct);
        }
        File.Move(vorlaeufig, pfad, overwrite: true);
    }

    private string Pfad(string nutzerId, string name)
        => Path.Combine(wurzel, Sicher(nutzerId), Sicher(name) + ".json");

    // Ein Nutzername kommt aus einer Eingabe. Ohne diese Pruefung waere
    // "../../etwas" ein Schreibzugriff ausserhalb des Ordners.
    private static string Sicher(string teil)
    {
        if (string.IsNullOrWhiteSpace(teil)
            || teil.Contains(Path.DirectorySeparatorChar)
            || teil.Contains(Path.AltDirectorySeparatorChar)
            || teil.Contains("..") || teil.Contains(':')
            || teil.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"Unzulaessiger Name: '{teil}'.", nameof(teil));
        }
        return teil;
    }
}
