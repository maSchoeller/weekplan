using Microsoft.AspNetCore.Mvc;
using Weekplan.Core.Anmeldung.Contracts;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Server;

public sealed record AnmeldeAnfrage(string Benutzername, string Passwort);
public sealed record AnmeldeAntwort(string Merkmal);

public static class Endpunkte
{
    internal const string AnmeldeGrenze = "anmeldung";
    internal const string StammdatenGrenze = "stammdaten";
    internal const string McpGrenze = "mcp";

    /// <summary>
    /// Die festen Daten: Rezepte, Trainingsphasen, Grundstock. Bewusst ohne
    /// Anmeldung — sie gehoeren keinem Nutzer und sind kein Geheimnis. Mit ETag,
    /// damit der Client seinen Zwischenspeicher behalten kann, und mengen-
    /// begrenzt, weil die Adresse oeffentlich ist.
    /// </summary>
    public static void MapStammdaten(this WebApplication app) =>
        app.MapGet("/stammdaten", async (
            HttpContext ctx, Stammdatenausgabe ausgabe, CancellationToken ct) =>
        {
            var stand = await ausgabe.StandAsync(ct);

            // Das Kennzeichen gehoert auch an die 304 — sonst verliert der
            // Client es beim ersten unveraenderten Abruf wieder.
            ctx.Response.Headers.ETag = stand.ETag;

            return ctx.Request.Headers.IfNoneMatch.Contains(stand.ETag)
                ? Results.StatusCode(StatusCodes.Status304NotModified)
                : Results.Bytes(stand.Json, "application/json");
        })
        .RequireRateLimiting(StammdatenGrenze);


    public static void MapAnmeldung(this WebApplication app) =>
        app.MapPost("/anmeldung", async (
            [FromBody] AnmeldeAnfrage anfrage,
            ITagebuch tagebuch,
            IPasswoerter passwoerter,
            IMerkmale merkmale,
            CancellationToken ct) =>
        {
            var konto = await tagebuch.KontoAsync(anfrage.Benutzername, ct);

            // Bewusst dieselbe Antwort fuer „kein solches Konto" und „falsches
            // Passwort": sonst verraet der Server, welche Namen es gibt.
            if (konto is null || !passwoerter.Stimmt(konto.PasswortHash, anfrage.Passwort))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new AnmeldeAntwort(merkmale.Erzeugen(konto.NutzerId)));
        })
        .RequireRateLimiting(AnmeldeGrenze);

    public static void MapTagebuch(this WebApplication app)
    {
        var gruppe = app.MapGroup("/tagebuch").AddEndpointFilter<NurAngemeldet>();

        gruppe.MapGet("/profil", (HttpContext ctx, ITagebuch tagebuch, CancellationToken ct)
            => tagebuch.ProfilAsync(ctx.NutzerId(), ct));

        gruppe.MapPut("/profil", async (
            HttpContext ctx, [FromBody] ProfilStand profil, ITagebuch tagebuch, CancellationToken ct) =>
        {
            await tagebuch.ProfilSpeichernAsync(ctx.NutzerId(), profil, ct);
            return Results.NoContent();
        });

        gruppe.MapGet("/woche", (HttpContext ctx, ITagebuch tagebuch, CancellationToken ct)
            => tagebuch.WocheAsync(ctx.NutzerId(), ct));

        gruppe.MapPut("/woche", async (
            HttpContext ctx, [FromBody] WochenStand woche, ITagebuch tagebuch, CancellationToken ct) =>
        {
            await tagebuch.WocheSpeichernAsync(ctx.NutzerId(), woche, ct);
            return Results.NoContent();
        });
    }
}
