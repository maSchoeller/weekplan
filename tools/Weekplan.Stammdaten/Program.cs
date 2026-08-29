using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Stammdaten;
using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Stammdaten;

// Die Rezepte, Trainingsphasen und der Grundstock lagen bis zum Lauf
// 2026-08-28 als Dateien beim Client. Dieses Werkzeug bringt sie einmal in die
// Datenbank — und prueft danach nach, dass alles angekommen ist.
//
//   dotnet run --project tools/Weekplan.Stammdaten -- [zielordner]
//
// Gegen die ausgerollte App: die Cosmos-Verbindung in WEEKPLAN_COSMOS setzen,
// dann schreibt das Werkzeug dorthin und der Ordner spielt keine Rolle mehr.
//
//   $env:WEEKPLAN_COSMOS = (az cosmosdb keys list -n cosmos-weekplan-prod `
//       -g rg-weekplan-prod --type connection-strings `
//       --query "connectionStrings[0].connectionString" -o tsv)

if (args.Length > 1)
{
    Console.Error.WriteLine("Aufruf: Weekplan.Stammdaten [zielordner]");
    Console.Error.WriteLine("Mit WEEKPLAN_COSMOS in der Umgebung geht es stattdessen nach Cosmos.");
    return 1;
}

var ordner = args.Length == 1
    ? args[0]
    : Path.Combine(Directory.GetCurrentDirectory(), "src", "Weekplan.Server", "stammdaten");

// Der Altbestand liegt neben dem Werkzeug und wird mit ihm ausgeliefert.
var altbestand = Path.Combine(AppContext.BaseDirectory, "altbestand");

// Dieselbe Regel wie im Server: liegt eine Cosmos-Verbindung vor, gilt sie.
var cosmos = Environment.GetEnvironmentVariable("WEEKPLAN_COSMOS");
var nachCosmos = !string.IsNullOrWhiteSpace(cosmos);

var dienste = new ServiceCollection();
if (nachCosmos)
{
    dienste.AddStammdatenInCosmos(cosmos!, "weekplan",
        Environment.GetEnvironmentVariable("WEEKPLAN_STAMMDATEN_BEHAELTER") ?? "stammdaten");
}
else
{
    dienste.AddStammdatenInDateien(ordner);
}

await using var anbieter = dienste.BuildServiceProvider();
var quelle = anbieter.GetRequiredService<IStammdaten>();

var satz = await Altbestand.LesenAsync(altbestand);
Console.WriteLine($"Gelesen: {satz.Rezepte.Rezepte.Count} Rezepte, "
                  + $"{satz.Training.Phasen.Count} Phasen, {satz.Grundstock.Gruppen.Count} Grundstockgruppen.");

await quelle.BefuellenAsync(satz);
Console.WriteLine(nachCosmos ? "Geschrieben nach Cosmos." : $"Geschrieben nach {ordner}.");

// Der eigentliche Punkt: zuruecklesen und Feld fuer Feld vergleichen. Ein Umzug,
// den niemand nachprueft, ist kein Nachweis.
var gelesen = await quelle.AllesAsync();
var klagen = Rueckvergleich.Vergleichen(satz, gelesen);

if (klagen.Count > 0)
{
    Console.Error.WriteLine($"Der Rueckvergleich meldet {klagen.Count} Abweichungen:");
    foreach (var klage in klagen) Console.Error.WriteLine($"  - {klage}");
    return 1;
}

Console.WriteLine($"Rueckvergleich in Ordnung: {gelesen.Rezepte.Rezepte.Count} Rezepte "
                  + "stimmen in Name, Kategorie, Zeit, kcal, Protein, Anleitung und jeder Zutat ueberein.");
return 0;
