using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Weekplan.Client;
using Weekplan.Client.Dienste;
using Weekplan.Core.Rechnen;
using Weekplan.Core.Wochenplanung;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Seit dem Lauf 2026-08-28 kommt alles vom Server: die Daten des Nutzers und
// die Stammdaten. Der Client liefert keine Datendateien mehr aus.
var serverAdresse = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl fehlt in wwwroot/appsettings.json.");
var server = new HttpClient { BaseAddress = new Uri(serverAdresse.TrimEnd('/') + "/") };

builder.Services.AddScoped(sp => new Stammdatenlader(server, sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddScoped<Sitzung>();
builder.Services.AddScoped(sp => new WeekplanApi(server, sp.GetRequiredService<Sitzung>()));
builder.Services.AddRechnen();
builder.Services.AddWochenplanung();
builder.Services.AddScoped<Zustand>();

await builder.Build().RunAsync();
