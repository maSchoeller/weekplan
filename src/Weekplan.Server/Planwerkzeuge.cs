using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Server;

/// <summary>
/// Die Werkzeuge fuer alles, was kein Rezept ist: Trainingsplan,
/// Verbrauchsrechnung, Grundstock und die Abteilungsliste. Getrennt von
/// <see cref="Rezeptwerkzeuge"/>, weil elf Werkzeuge in einem Rezepttyp kein
/// Rezepttyp mehr waeren.
///
/// <para>
/// Jedes Schreibwerkzeug <b>ersetzt sein Dokument vollstaendig</b> — dieselbe
/// Form wie <c>rezept_aendern</c>, und aus demselben Grund: keine Patch-Sprache,
/// keine Konfliktaufloesung, keine zweite Denkweise.
/// </para>
///
/// <para>
/// <b>Das Regelwerk bleibt lesend.</b> Nicht per Absprache, sondern weil
/// <see cref="Trainingsentwurf"/> gar kein Regelfeld hat: es gibt keinen Weg,
/// Regeln zu uebergeben.
/// </para>
/// </summary>
[McpServerToolType]
public sealed class Planwerkzeuge(IStammdaten quelle, Stammdatenausgabe ausgabe, ILogger<Planwerkzeuge> protokoll)
{
    [McpServerTool(Name = "training_lesen")]
    [Description("Trainingsphasen, MET-Werte, Kraftplan und Regelwerk. Vor jedem training_schreiben "
                 + "lesen — geschrieben wird der ganze Plan, nicht ein Teil davon.")]
    public async Task<Trainingsdaten> TrainingAsync(CancellationToken ct)
        => (await quelle.AllesAsync(ct)).Training;

    [McpServerTool(Name = "training_schreiben")]
    [Description("Ersetzt den Trainingsplan vollstaendig: Hinweis, MET-Werte, Phasen und Kraftplan. "
                 + "Vorher training_lesen, damit nichts verlorengeht. Das Regelwerk laesst sich hier "
                 + "nicht uebergeben und bleibt unveraendert. MET-Werte muessen mindestens 1 sein — "
                 + "die Rechnung (MET minus 1) ergaebe sonst einen negativen Verbrauch. Jede Einheit "
                 + "einer Phase muss einen MET-Typ nennen, den es in metWerte gibt.")]
    public async Task<Trainingsdaten> TrainingSchreibenAsync(Trainingsentwurf training, CancellationToken ct)
    {
        var neu = await Durchreichen(() => quelle.TrainingSchreibenAsync(training, ct));
        ausgabe.Verwerfen();
        protokoll.LogInformation(
            "MCP: Trainingsplan geschrieben ({Phasen} Phasen, {Met} MET-Werte).",
            neu.Phasen.Count, neu.MetWerte.Count);
        return neu;
    }

    [McpServerTool(Name = "grundstock_lesen")]
    [Description("Der Vorratseinkauf. Was hier steht, gehoert in einem Rezept mit vorrat=true "
                 + "gekennzeichnet, damit es nicht auf der Wochenliste landet.")]
    public async Task<Grundstockdaten> GrundstockAsync(CancellationToken ct)
        => (await quelle.AllesAsync(ct)).Grundstock;

    [McpServerTool(Name = "grundstock_schreiben")]
    [Description("Ersetzt den Grundstock vollstaendig. Vorher grundstock_lesen. Jeder Artikel "
                 + "braucht Namen und Menge — ohne Menge stuende man im Laden und wuesste nicht, wie viel.")]
    public async Task<Grundstockdaten> GrundstockSchreibenAsync(
        Grundstockdaten grundstock, CancellationToken ct)
    {
        var neu = await Durchreichen(() => quelle.GrundstockSchreibenAsync(grundstock, ct));
        ausgabe.Verwerfen();
        protokoll.LogInformation("MCP: Grundstock geschrieben ({Gruppen} Gruppen).", neu.Gruppen.Count);
        return neu;
    }

    [McpServerTool(Name = "abteilungen_lesen")]
    [Description("Die erlaubten Supermarkt-Abteilungen, in der Reihenfolge der Einkaufsliste. "
                 + "Jede Zutat muss eine davon nennen.")]
    public async Task<IReadOnlyList<string>> AbteilungenAsync(CancellationToken ct)
        => (await quelle.AllesAsync(ct)).Rezepte.Abteilungen;

    [McpServerTool(Name = "abteilungen_schreiben")]
    [Description("Ersetzt die Abteilungsliste vollstaendig. Die Reihenfolge ist der Weg durch den "
                 + "Laden. Faellt eine Abteilung weg, in der noch Zutaten stehen, wandern diese "
                 + "Zutaten nach 'Sonstiges' ans Ende der Liste — kein Rezept wird ungueltig. Die "
                 + "Antwort nennt, wie viele Zutaten das betraf.")]
    public async Task<string> AbteilungenSchreibenAsync(Abteilungsentwurf abteilungen, CancellationToken ct)
    {
        var umzug = await Durchreichen(() => quelle.AbteilungenSchreibenAsync(abteilungen, ct));
        ausgabe.Verwerfen();
        protokoll.LogInformation(
            "MCP: Abteilungen geschrieben ({Anzahl} Abteilungen, {Zutaten} Zutaten umgezogen).",
            umzug.Abteilungen.Abteilungen.Count, umzug.Zutaten);

        var liste = string.Join(", ", umzug.Abteilungen.Abteilungen);

        return umzug.Zutaten == 0
            ? $"Abteilungen gesetzt: {liste}. Keine Zutat war betroffen."
            : $"Abteilungen gesetzt: {liste}. {umzug.Zutaten} "
              + $"{(umzug.Zutaten == 1 ? "Zutat" : "Zutaten")} in {umzug.Rezepte} "
              + $"{(umzug.Rezepte == 1 ? "Rezept" : "Rezepten")} nach 'Sonstiges' verschoben.";
    }

    /// <summary>
    /// Wie in <see cref="Rezeptwerkzeuge"/>: ohne diese Umhuellung meldet das SDK
    /// nur „An error occurred invoking ...", und der Aufrufer erfaehrt nie, was
    /// er falsch gemacht hat.
    /// </summary>
    private static async Task<T> Durchreichen<T>(Func<Task<T>> tun)
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
