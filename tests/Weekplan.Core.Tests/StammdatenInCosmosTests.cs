using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Stammdaten;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Derselbe Vertrag wie <see cref="StammdatenInDateienTests"/>, aber gegen eine
/// echte Cosmos-Datenbank — eine Attrappe wuerde weder Serialisierung noch
/// Partitionsschluessel noch die Abfrage ueber alle Rezepte beruehren.
///
/// <para>
/// **Laeuft nicht im Standardlauf** (Merkmal <c>Ablage=Cosmos</c>). Er braucht
/// **zwei** Umgebungsvariablen, und das mit Absicht: die Rezeptkennungen sind je
/// Lauf eindeutig, die Dokumente <c>training</c>, <c>grundstock</c> und
/// <c>abteilungen</c> sind es aber **nicht**. Gegen den echten Behaelter
/// gerichtet wuerde dieser Test sie ueberschreiben. Darum nennt
/// <c>WEEKPLAN_STAMMDATEN_BEHAELTER</c> den Behaelter ausdruecklich, und es gibt
/// keinen Vorgabewert.
/// </para>
/// <code>
/// $env:WEEKPLAN_COSMOS = (az cosmosdb keys list -n cosmos-weekplan-prod `
///     -g rg-weekplan-prod --type connection-strings `
///     --query "connectionStrings[0].connectionString" -o tsv)
/// $env:WEEKPLAN_STAMMDATEN_BEHAELTER = "stammdaten-test"
/// dotnet test Weekplan.slnx --filter "Ablage=Cosmos"
/// </code>
/// </summary>
[Trait("Ablage", "Cosmos")]
public sealed class StammdatenInCosmosTests : StammdatenVertrag, IDisposable
{
    private const string Datenbank = "weekplan";

    private readonly List<ServiceProvider> _dienste = [];

    private static string Verbindung =>
        Environment.GetEnvironmentVariable("WEEKPLAN_COSMOS")
        ?? throw new InvalidOperationException(
            "WEEKPLAN_COSMOS ist nicht gesetzt — siehe den Kommentar an StammdatenInCosmosTests.");

    private static string Behaelter =>
        Environment.GetEnvironmentVariable("WEEKPLAN_STAMMDATEN_BEHAELTER")
        ?? throw new InvalidOperationException(
            "WEEKPLAN_STAMMDATEN_BEHAELTER ist nicht gesetzt. Ohne ihn liefe dieser Test "
            + "gegen den echten Behaelter und wuerde Training, Grundstock und Abteilungen "
            + "ueberschreiben — siehe den Kommentar an StammdatenInCosmosTests.");

    protected override IStammdaten Quelle()
    {
        // Jeder Aufruf baut eine frische Quelle: der Vertrag prueft mit der
        // zweiten auch, dass wirklich geschrieben wurde.
        var anbieter = new ServiceCollection()
            .AddStammdatenInCosmos(Verbindung, Datenbank, Behaelter)
            .BuildServiceProvider();
        _dienste.Add(anbieter);
        return anbieter.GetRequiredService<IStammdaten>();
    }

    // Der CosmosClient haelt Verbindungen offen; ohne Aufraeumen haengt der
    // Testlauf am Ende sekundenlang.
    public void Dispose()
    {
        foreach (var anbieter in _dienste) anbieter.Dispose();
    }
}
