using Weekplan.Core.Rechnen;

var builder = WebApplication.CreateBuilder(args);

// Client und Server liegen auf verschiedenen Herkuenften (Static Web Apps bzw.
// Container Apps). Ohne diese Liste beantwortet der Browser keine Anfrage.
const string ClientPolicy = "client";
var clientOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(ClientPolicy, policy => policy
    .WithOrigins(clientOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddRechnen();

var app = builder.Build();

app.UseCors(ClientPolicy);

app.MapGet("/health", () => Results.Ok(new HealthAntwort("ok", "weekplan-server")));

app.Run();

internal sealed record HealthAntwort(string Status, string Dienst);
