using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Weekplan.Client;
using Weekplan.Client.Dienste;
using Weekplan.Core.Rechnen;
using Weekplan.Core.Wochenplanung;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Zwei Herkuenfte, zwei Clients: die festen Dateien liegen beim Client selbst,
// die Daten des Nutzers auf dem Server.
var eigene = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

var serverAdresse = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl fehlt in wwwroot/appsettings.json.");
var server = new HttpClient { BaseAddress = new Uri(serverAdresse.TrimEnd('/') + "/") };

builder.Services.AddScoped(_ => new Stammdatenlader(eigene));
builder.Services.AddScoped<Sitzung>();
builder.Services.AddScoped(sp => new WeekplanApi(server, sp.GetRequiredService<Sitzung>()));
builder.Services.AddRechnen();
builder.Services.AddWochenplanung();
builder.Services.AddScoped<Zustand>();

await builder.Build().RunAsync();
