using Microsoft.Extensions.Hosting;

namespace Weekplan.Server.Tests;

/// <summary>
/// Wie <see cref="ServerFixture"/>, aber mit konfiguriertem MCP-Schluessel —
/// also mit eingehaengtem Pflegeweg. Zwei Fixtures, weil genau der Unterschied
/// geprueft werden soll: ohne Schluessel darf es den Endpunkt nicht geben.
/// </summary>
public sealed class McpServerFixture : ServerFixture
{
    public const string McpSchluessel = "test-mcp-schluessel-fuer-die-tests";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mcp:Schluessel"] = McpSchluessel
        }));
        return base.CreateHost(builder);
    }
}
