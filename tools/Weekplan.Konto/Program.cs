using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Anmeldung;
using Weekplan.Core.Anmeldung.Contracts;
using Weekplan.Core.Tagebuch;
using Weekplan.Core.Tagebuch.Contracts;

// weekplan hat keine Registrierungsseite — genau deshalb kann sich niemand
// anlegen, der die Adresse kennt. Das Konto entsteht hier, von Hand.
//
//   dotnet run --project tools/Weekplan.Konto -- <benutzername> <passwort> [ordner]
//
// Gegen die ausgerollte App: die Cosmos-Verbindung in WEEKPLAN_COSMOS setzen,
// dann schreibt das Werkzeug dorthin und der Ordner spielt keine Rolle mehr.
//
//   $env:WEEKPLAN_COSMOS = (az cosmosdb keys list -n cosmos-weekplan-prod `
//       -g rg-weekplan-prod --type connection-strings `
//       --query "connectionStrings[0].connectionString" -o tsv)

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("Aufruf: Weekplan.Konto <benutzername> <passwort> [datenordner]");
    Console.Error.WriteLine("Mit WEEKPLAN_COSMOS in der Umgebung geht es stattdessen nach Cosmos.");
    return 1;
}

var (benutzername, passwort) = (args[0], args[1]);
var ordner = args.Length == 3
    ? args[2]
    : Path.Combine(Directory.GetCurrentDirectory(), "src", "Weekplan.Server", "daten");

if (passwort.Length < 8)
{
    Console.Error.WriteLine("Das Passwort braucht mindestens acht Zeichen.");
    return 1;
}

// Dieselbe Regel wie im Server: liegt eine Cosmos-Verbindung vor, gilt sie.
var cosmos = Environment.GetEnvironmentVariable("WEEKPLAN_COSMOS");
var nachCosmos = !string.IsNullOrWhiteSpace(cosmos);

var sammlung = new ServiceCollection();
if (nachCosmos)
{
    sammlung.AddTagebuchInCosmos(cosmos!, "weekplan", "tagebuch");
}
else
{
    sammlung.AddTagebuchInDateien(ordner);
}

await using var dienste = sammlung
    // Der Schluessel wird hier nur gebraucht, weil der Slice ihn verlangt; das
    // Werkzeug stellt keine Merkmale aus, es hasht nur.
    .AddAnmeldung("werkzeug-hasht-nur-und-stellt-keine-merkmale-aus")
    .BuildServiceProvider();

var tagebuch = dienste.GetRequiredService<ITagebuch>();
var passwoerter = dienste.GetRequiredService<IPasswoerter>();
var nutzerId = TagebuchServiceCollectionExtensions.NutzerIdVon(benutzername);

try
{
    await tagebuch.KontoAnlegenAsync(new Konto(nutzerId, benutzername, passwoerter.Hashen(passwort)));
}
catch (InvalidOperationException fehler)
{
    Console.Error.WriteLine(fehler.Message);
    return 1;
}

var wo = nachCosmos ? "in Cosmos (weekplan/tagebuch)" : $"unter {ordner}";
Console.WriteLine($"Konto '{benutzername}' angelegt (Kennung '{nutzerId}') {wo}");
return 0;
