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
/// Alles ausser den Rezepten liegt seit dem Lauf 2026-08-29 bei
/// <see cref="Planwerkzeuge"/> — auch das Lesen von Training, Grundstock und
/// Abteilungen, das frueher hier stand. Die Trennung verlaeuft am Gegenstand,
/// nicht an Lesen und Schreiben.
/// </para>
/// </summary>
[McpServerToolType]
public sealed class Rezeptwerkzeuge(
    IStammdaten quelle, Stammdatenausgabe ausgabe, ILogger<Rezeptwerkzeuge> protokoll)
{
    /// <summary>Die Kurzform fuer Listen — ohne Anleitung, die ist lang.</summary>
    public sealed record Rezeptzeile(
        string Id, string Name, string Kategorie, int Kcal, int Protein, int ZeitMin,
        bool Kalt, bool Prep);

    [McpServerTool(Name = "rezepte_auflisten")]
    [Description("Listet alle Rezepte mit Kennung, Name, Kategorie, kcal, Protein, Zeit sowie den "
                 + "Merkmalen kalt und prep — ohne die Anleitung. Der Einstieg, bevor man etwas "
                 + "liest oder aendert.")]
    public async Task<IReadOnlyList<Rezeptzeile>> AuflistenAsync(CancellationToken ct)
    {
        var alles = await quelle.AllesAsync(ct);
        return [.. alles.Rezepte.Rezepte.Select(r =>
            new Rezeptzeile(r.Id, r.Name, r.Kategorie, r.Kcal, r.Protein, r.ZeitMin, r.Kalt, r.Prep))];
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
                 + "auf der Wochenliste. kalt=true heisst: schmeckt auch kalt. prep=true heisst: haelt "
                 + "drei Tage im Kuehlschrank und waermt gut auf — danach waehlt die App die Gerichte "
                 + "fuer die Werktage aus. Die Anleitung ist Markdown: Zwischenueberschriften, Listen "
                 + "und Tabellen sind erlaubt, Bilder und eingebettetes HTML nicht.")]
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
        catch (StammdatenUngueltigException fehler)
        {
            throw new McpException(fehler.Message, fehler);
        }
    }
}
