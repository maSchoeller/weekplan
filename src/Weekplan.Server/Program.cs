using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Weekplan.Core.Anmeldung;
using Weekplan.Core.Rechnen;
using Weekplan.Core.Stammdaten;
using Weekplan.Core.Tagebuch;
using Weekplan.Server;

var builder = WebApplication.CreateBuilder(args);

// Client und Server liegen auf verschiedenen Herkuenften (Static Web Apps bzw.
// Container Apps). Ohne diese Liste beantwortet der Browser keine Anfrage.
const string ClientPolicy = "client";
var clientOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(ClientPolicy, policy => policy
    .WithOrigins(clientOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Ueber eine Herkunftsgrenze gibt der Browser von sich aus nur eine
    // Handvoll Kopfzeilen frei, und ETag ist nicht darunter. Ohne diese Zeile
    // liest der Client sein eigenes Kennzeichen nicht und laedt die Stammdaten
    // bei jeder Pruefung vollstaendig neu.
    .WithExposedHeaders("ETag")));

builder.Services.AddRechnen();
builder.Services.AddAnmeldung(builder.Configuration["Anmeldung:Schluessel"]
    ?? throw new InvalidOperationException(
        "Anmeldung:Schluessel fehlt. Ohne Signaturschluessel darf der Server nicht starten."));
// Wo die Daten liegen, entscheidet die Anwesenheit einer Cosmos-Verbindung —
// kein Schalter, der auch falsch stehen koennte. In Azure kommt sie als Secret
// herein; lokal steht sie nirgends, also legt der Server dort auf Dateien ab.
var cosmos = builder.Configuration["Tagebuch:Cosmos:Verbindung"];
if (!string.IsNullOrWhiteSpace(cosmos))
{
    builder.Services.AddTagebuchInCosmos(
        cosmos,
        builder.Configuration["Tagebuch:Cosmos:Datenbank"] ?? "weekplan",
        builder.Configuration["Tagebuch:Cosmos:Behaelter"] ?? "tagebuch");
}
else
{
    builder.Services.AddTagebuchInDateien(builder.Configuration["Tagebuch:Ordner"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "daten"));
}

// Die Stammdaten liegen dort, wo auch das Tagebuch liegt — entschieden wieder
// allein durch die Anwesenheit einer Verbindung, nicht durch einen Schalter.
var stammdatenCosmos = builder.Configuration["Stammdaten:Cosmos:Verbindung"];
if (!string.IsNullOrWhiteSpace(stammdatenCosmos))
{
    builder.Services.AddStammdatenInCosmos(
        stammdatenCosmos,
        builder.Configuration["Stammdaten:Cosmos:Datenbank"] ?? "weekplan",
        builder.Configuration["Stammdaten:Cosmos:Behaelter"] ?? "stammdaten");
}
else
{
    builder.Services.AddStammdatenInDateien(builder.Configuration["Stammdaten:Ordner"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "stammdaten"));
}
builder.Services.AddSingleton<Stammdatenausgabe>();

// Der Pflegeweg fuer die Rezepte. Ohne Schluessel in der Konfiguration entsteht
// er gar nicht — lokal ist er damit standardmaessig aus, und es gibt keinen
// Zustand, in dem er ungesichert offen steht.
var mcpSchluessel = builder.Configuration["Mcp:Schluessel"];
if (!string.IsNullOrWhiteSpace(mcpSchluessel))
{
    builder.Services.AddSingleton<Rezeptwerkzeuge>();
    builder.Services.AddMcpServer().WithHttpTransport().WithTools<Rezeptwerkzeuge>();
}

// Die Adresse ist oeffentlich, also darf niemand Passwoerter durchprobieren.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(Endpunkte.AnmeldeGrenze, grenze =>
    {
        grenze.PermitLimit = 10;
        grenze.Window = TimeSpan.FromMinutes(1);
        grenze.QueueLimit = 0;
    });

    // Die Stammdaten sind oeffentlich lesbar. Die Grenze ist weit genug fuer
    // jeden echten Gebrauch und eng genug, dass niemand den Server damit
    // beschaeftigt haelt.
    options.AddFixedWindowLimiter(Endpunkte.StammdatenGrenze, grenze =>
    {
        grenze.PermitLimit = 120;
        grenze.Window = TimeSpan.FromMinutes(1);
        grenze.QueueLimit = 0;
    });

    // Der Pflegeweg. Weit genug fuer ein Gespraech mit vielen Werkzeugrufen,
    // eng genug, dass ein durchgesickerter Schluessel nicht in Minuten alle
    // Rezepte umschreibt.
    options.AddFixedWindowLimiter(Endpunkte.McpGrenze, grenze =>
    {
        grenze.PermitLimit = 60;
        grenze.Window = TimeSpan.FromMinutes(1);
        grenze.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseCors(ClientPolicy);
app.UseRateLimiter();

// Vor allem anderen: wer keinen gueltigen Schluessel mitbringt, kommt an /mcp
// gar nicht erst bis zu einem Endpunkt.
if (!string.IsNullOrWhiteSpace(mcpSchluessel)) app.UseMcpSchluessel(mcpSchluessel);

app.MapGet("/health", () => Results.Ok(new HealthAntwort("ok", "weekplan-server")));
app.MapStammdaten();
app.MapAnmeldung();
app.MapTagebuch();

if (!string.IsNullOrWhiteSpace(mcpSchluessel))
{
    app.MapMcp("/mcp").RequireRateLimiting(Endpunkte.McpGrenze);
}

app.Run();

internal sealed record HealthAntwort(string Status, string Dienst);

// Damit die Integrationstests den Server hochfahren koennen.
public partial class Program;
