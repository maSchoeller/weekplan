using System.Net.Http.Json;
using Weekplan.Client.Daten;

namespace Weekplan.Client.Dienste;

/// <summary>Laedt die festen Dateien einmal je Sitzung.</summary>
public sealed class Stammdatenlader(HttpClient http)
{
    private Stammdaten? _geladen;

    public async Task<Stammdaten> LadenAsync()
    {
        if (_geladen is not null) return _geladen;

        var rezepte = http.GetFromJsonAsync<Rezeptdaten>("data/rezepte.json");
        var training = http.GetFromJsonAsync<Trainingsdaten>("data/training.json");
        var grundstock = http.GetFromJsonAsync<Grundstockdaten>("data/grundstock.json");
        await Task.WhenAll(rezepte, training, grundstock);

        return _geladen = new Stammdaten(
            await rezepte ?? throw new InvalidOperationException("rezepte.json fehlt."),
            await training ?? throw new InvalidOperationException("training.json fehlt."),
            await grundstock ?? throw new InvalidOperationException("grundstock.json fehlt."));
    }
}
