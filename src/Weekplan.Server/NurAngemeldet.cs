using Microsoft.Net.Http.Headers;
using Weekplan.Core.Anmeldung.Contracts;

namespace Weekplan.Server;

/// <summary>
/// Laesst nur durch, wer ein gueltiges Merkmal mitbringt, und legt den Nutzer
/// fuer den Endpunkt ab. Kein Cookie: das Merkmal kommt als Bearer-Kopfzeile,
/// weil Client und Server auf verschiedenen Herkuenften liegen.
/// </summary>
internal sealed class NurAngemeldet(IMerkmale merkmale) : IEndpointFilter
{
    private const string Praefix = "Bearer ";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var kopf = ctx.HttpContext.Request.Headers[HeaderNames.Authorization].ToString();
        var merkmal = kopf.StartsWith(Praefix, StringComparison.OrdinalIgnoreCase)
            ? kopf[Praefix.Length..]
            : null;

        if (await merkmale.NutzerAusAsync(merkmal) is not { } nutzerId)
        {
            return Results.Unauthorized();
        }

        ctx.HttpContext.Items[NutzerSchluessel] = nutzerId;
        return await next(ctx);
    }

    internal const string NutzerSchluessel = "weekplan.nutzer";
}

internal static class HttpContextErweiterungen
{
    /// <summary>Nur hinter <see cref="NurAngemeldet"/> gueltig.</summary>
    internal static string NutzerId(this HttpContext ctx)
        => (string)ctx.Items[NurAngemeldet.NutzerSchluessel]!;
}
