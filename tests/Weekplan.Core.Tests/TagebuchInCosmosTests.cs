using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Tagebuch;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Derselbe Vertrag wie <see cref="TagebuchInDateienTests"/>, aber gegen eine
/// echte Cosmos-Datenbank. Das ist der einzige Weg, die Umsetzung zu pruefen —
/// eine Attrappe wuerde weder die Serialisierung noch den Partitionsschluessel
/// noch das Verhalten bei „nicht gefunden" beruehren, und genau dort sitzen die
/// Fehler.
///
/// <para>
/// **Laeuft nicht im Standardlauf.** Er braucht eine Verbindung, die niemand im
/// Repo haben darf. Darum traegt die Klasse das Merkmal <c>Ablage=Cosmos</c>,
/// CI und Deploy schliessen es aus, und hier laeuft er von Hand:
/// </para>
/// <code>
/// $env:WEEKPLAN_COSMOS = (az cosmosdb keys list -n cosmos-weekplan-prod `
///     -g rg-weekplan-prod --type connection-strings `
///     --query "connectionStrings[0].connectionString" -o tsv)
/// dotnet test Weekplan.slnx --filter "Ablage=Cosmos"
/// </code>
/// <para>
/// Die Nutzerkennungen sind je Lauf eindeutig (siehe <see cref="TagebuchVertrag"/>),
/// er kann also gegen dieselbe Datenbank laufen, die die App benutzt, ohne ihr
/// ins Gehege zu kommen.
/// </para>
/// </summary>
[Trait("Ablage", "Cosmos")]
public sealed class TagebuchInCosmosTests : TagebuchVertrag, IDisposable
{
    private const string Datenbank = "weekplan";
    private const string Behaelter = "tagebuch";

    private readonly List<ServiceProvider> _dienste = [];

    private static string Verbindung =>
        Environment.GetEnvironmentVariable("WEEKPLAN_COSMOS")
        ?? throw new InvalidOperationException(
            "WEEKPLAN_COSMOS ist nicht gesetzt. Dieser Test braucht eine echte "
            + "Cosmos-Verbindung — siehe den Kommentar an TagebuchInCosmosTests.");

    protected override ITagebuch Tagebuch()
    {
        // Jeder Aufruf baut ein frisches Tagebuch, denn der Vertrag prueft mit
        // dem zweiten auch, dass wirklich geschrieben wurde und nichts nur im
        // Arbeitsspeicher stand.
        var anbieter = new ServiceCollection()
            .AddTagebuchInCosmos(Verbindung, Datenbank, Behaelter)
            .BuildServiceProvider();
        _dienste.Add(anbieter);
        return anbieter.GetRequiredService<ITagebuch>();
    }

    // Der CosmosClient haelt Verbindungen offen; ohne dieses Aufraeumen haengt
    // der Testlauf am Ende sekundenlang.
    public void Dispose()
    {
        foreach (var anbieter in _dienste) anbieter.Dispose();
    }
}
