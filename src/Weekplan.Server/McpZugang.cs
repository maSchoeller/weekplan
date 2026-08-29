using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Weekplan.Server;

/// <summary>
/// Der einzige Schreibweg von aussen. Er ist durch einen langlebigen Schluessel
/// gesichert, der als Container-App-Secret liegt und mit dem Anmelde-Merkmal
/// der App nichts zu tun hat: ein durchgesickerter MCP-Schluessel oeffnet keine
/// Nutzerdaten, und er laesst sich einzeln austauschen.
///
/// <para>
/// Fehlt der Schluessel in der Konfiguration, wird <c>/mcp</c> gar nicht erst
/// eingehaengt — es gibt keinen Zustand, in dem der Endpunkt offen steht.
/// </para>
/// </summary>
internal static class McpZugang
{
    public static void UseMcpSchluessel(this WebApplication app, string schluessel)
    {
        var erwartet = SHA256.HashData(Encoding.UTF8.GetBytes(schluessel));

        app.Use(async (ctx, weiter) =>
        {
            if (!ctx.Request.Path.StartsWithSegments("/mcp"))
            {
                await weiter(ctx);
                return;
            }

            if (!Stimmt(ctx.Request.Headers.Authorization, erwartet))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.Headers.WWWAuthenticate = "Bearer";
                return;
            }

            await weiter(ctx);
        });
    }

    /// <summary>
    /// Verglichen wird ueber die Hashes und in fester Zeit: sonst verriete die
    /// Dauer des Vergleichs, wie viele Zeichen am Anfang schon stimmen.
    /// </summary>
    private static bool Stimmt(StringValues kopfzeile, byte[] erwartet)
    {
        if (kopfzeile.Count != 1) return false;

        var wert = kopfzeile[0];
        const string Vorsatz = "Bearer ";

        if (wert is null || !wert.StartsWith(Vorsatz, StringComparison.Ordinal)) return false;

        var gegeben = SHA256.HashData(Encoding.UTF8.GetBytes(wert[Vorsatz.Length..]));
        return CryptographicOperations.FixedTimeEquals(gegeben, erwartet);
    }
}
