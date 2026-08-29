using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Server;

/// <summary>
/// Die Werkzeuge, mit denen Claude Code die Rezepte pflegt. Deutsch benannt wie
/// der uebrige Code — sie arbeiten auf deutsch benannten Feldern eines deutsch
/// benannten Modells, englische Namen davor waeren der einzige Bruch im Repo.
///
/// <para>
/// <b>Schreiben darf nur, was Rezept ist.</b> Trainingsphasen, MET-Werte,
/// Grundstock und die Abteilungsliste sind Rechengrundlage und bleiben dem
/// Commit vorbehalten: eine verrutschte MET-Zahl verschoebe still jede
/// Kalorienzahl der App. Lesen darf man alles — ohne den Grundstock liesse sich
/// nicht entscheiden, ob eine Zutat Vorrat ist, und ohne die Abteilungen
/// muesste man sie erraten.
/// </para>
/// </summary>
[McpServerToolType]
public sealed class Rezeptwerkzeuge(
    IStammdaten quelle, Stammdatenausgabe ausgabe, ILogger<Rezeptwerkzeuge> protokoll)
{
    /// <summary>Die Kurzform fuer Listen — ohne Anleitung, die ist lang.</summary>
    public sealed record Rezeptzeile(
        string Id, string Name, string Kategorie, int Kcal, int Protein, int ZeitMin, bool Kalt);

    [McpServerTool(Name = "rezepte_auflisten")]
    [Description("Listet alle Rezepte mit Kennung, Name, Kategorie, kcal, Protein und Zeit — "
                 + "ohne die Anleitung. Der Einstieg, bevor man etwas liest oder aendert.")]
    public async Task<IReadOnlyList<Rezeptzeile>> AuflistenAsync(CancellationToken ct)
    {
        var alles = await quelle.AllesAsync(ct);
        return [.. alles.Rezepte.Rezepte.Select(r =>
            new Rezeptzeile(r.Id, r.Name, r.Kategorie, r.Kcal, r.Protein, r.ZeitMin, r.Kalt))];
    }

    [McpServerTool(Name = "rezept_lesen")]
    [Description("Liest ein vollstaendiges Rezept samt Zutaten und Markdown-Anleitung.")]
    public async Task<Rezept> LesenAsync(
        [Description("Die Kennung des Rezepts, etwa chili-sin-carne.")] string id,
        CancellationToken ct)
        => await quelle.RezeptAsync(id, ct)
           ?? throw new McpException(
               $"Es gibt kein Rezept mit der Kennung '{id}'. rezepte_auflisten zeigt die vorhandenen.");

    [McpServerTool(Name = "rezept_anlegen")]
    [Description("Legt ein neues Rezept an. Die Kennung entsteht aus dem Namen; gibt es sie schon, "
                 + "ist das ein Fehler — zum Ersetzen rezept_aendern nehmen. Mengen gelten je Portion. "
                 + "Zutaten, die im Grundstock stehen, mit vorrat=true kennzeichnen, sonst landen sie "
                 + "auf der Wochenliste. Die Anleitung ist Markdown: Zwischenueberschriften, Listen und "
                 + "Tabellen sind erlaubt, Bilder und eingebettetes HTML nicht.")]
    public async Task<Rezept> AnlegenAsync(Rezeptentwurf rezept, CancellationToken ct)
    {
        var angelegt = await Durchreichen(() => quelle.AnlegenAsync(rezept, ct));
        ausgabe.Verwerfen();
        protokoll.LogInformation("MCP: Rezept {Kennung} angelegt ({Name}).", angelegt.Id, angelegt.Name);
        return angelegt;
    }

    [McpServerTool(Name = "rezept_aendern")]
    [Description("Ersetzt ein vorhandenes Rezept vollstaendig. Die Kennung muss es geben — "
                 + "aendern legt nicht an. Vorher rezept_lesen, damit nichts verlorengeht.")]
    public async Task<Rezept> AendernAsync(
        [Description("Die Kennung des zu aendernden Rezepts.")] string id,
        Rezeptentwurf rezept,
        CancellationToken ct)
    {
        var geaendert = await Durchreichen(() => quelle.AendernAsync(id, rezept, ct));
        ausgabe.Verwerfen();
        protokoll.LogInformation("MCP: Rezept {Kennung} geaendert ({Name}).", geaendert.Id, geaendert.Name);
        return geaendert;
    }

    [McpServerTool(Name = "rezept_loeschen")]
    [Description("Loescht ein Rezept endgueltig. Steht es noch in einem Wochenplan, zeigt die App "
                 + "dort seinen Namen mit dem Vermerk entfernt — verloren geht der Plan nicht.")]
    public async Task<string> LoeschenAsync(
        [Description("Die Kennung des zu loeschenden Rezepts.")] string id,
        CancellationToken ct)
    {
        if (!await quelle.LoeschenAsync(id, ct))
        {
            protokoll.LogInformation("MCP: Rezept {Kennung} sollte geloescht werden, gab es aber nicht.", id);
            return $"Es gab kein Rezept mit der Kennung '{id}'.";
        }

        ausgabe.Verwerfen();
        protokoll.LogInformation("MCP: Rezept {Kennung} geloescht.", id);
        return $"Rezept '{id}' geloescht.";
    }

    /// <summary>
    /// Die Absage muss beim Aufrufer ankommen — sie nennt die erlaubten Werte,
    /// und nur damit kann er korrigieren statt zu raten. Ohne diese Umhuellung
    /// meldet das SDK nur „An error occurred invoking ...", und das Rezept
    /// laesst sich nie anlegen.
    /// </summary>
    private static async Task<Rezept> Durchreichen(Func<Task<Rezept>> tun)
    {
        try
        {
            return await tun();
        }
        catch (RezeptUngueltigException fehler)
        {
            throw new McpException(fehler.Message, fehler);
        }
    }

    [McpServerTool(Name = "abteilungen_lesen")]
    [Description("Die erlaubten Supermarkt-Abteilungen, in der Reihenfolge der Einkaufsliste. "
                 + "Jede Zutat muss eine davon nennen. Nur lesbar: die Reihenfolge ist der Weg "
                 + "durch den Laden und wird per Commit geaendert.")]
    public async Task<IReadOnlyList<string>> AbteilungenAsync(CancellationToken ct)
        => (await quelle.AllesAsync(ct)).Rezepte.Abteilungen;

    [McpServerTool(Name = "grundstock_lesen")]
    [Description("Der Vorratseinkauf. Was hier steht, gehoert in einem Rezept mit vorrat=true "
                 + "gekennzeichnet, damit es nicht auf der Wochenliste landet. Nur lesbar.")]
    public async Task<Grundstockdaten> GrundstockAsync(CancellationToken ct)
        => (await quelle.AllesAsync(ct)).Grundstock;

    [McpServerTool(Name = "training_lesen")]
    [Description("Trainingsphasen, MET-Werte und Kraftplan. Nur lesbar — sie sind Rechengrundlage "
                 + "der ganzen App und werden per Commit geaendert, nicht im Gespraech.")]
    public async Task<Trainingsdaten> TrainingAsync(CancellationToken ct)
        => (await quelle.AllesAsync(ct)).Training;
}
