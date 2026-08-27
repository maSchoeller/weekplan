using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Core.Tagebuch;

/// <summary>Einziger Eingang in den Slice — die Umsetzungen bleiben <c>internal</c>.</summary>
public static class TagebuchServiceCollectionExtensions
{
    /// <summary>Ablage als JSON-Dateien unter <paramref name="ordner"/>: lokal und im Smoketest.</summary>
    public static IServiceCollection AddTagebuchInDateien(this IServiceCollection services, string ordner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ordner);
        services.AddSingleton<IAblage>(_ => new DateiAblage(ordner));
        services.AddSingleton<ITagebuch, Tagebuch>();
        return services;
    }

    /// <summary>
    /// Ablage in einem Cosmos-Container: in Azure. Der Container muss es schon
    /// geben — angelegt wird er beim Ausrollen, nicht beim Starten.
    /// </summary>
    public static IServiceCollection AddTagebuchInCosmos(
        this IServiceCollection services, string verbindung, string datenbank, string behaelter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verbindung);
        ArgumentException.ThrowIfNullOrWhiteSpace(datenbank);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaelter);
        services.AddSingleton<IAblage>(_ => new CosmosAblage(verbindung, datenbank, behaelter));
        services.AddSingleton<ITagebuch, Tagebuch>();
        return services;
    }

    /// <summary>Die Nutzerkennung zu einem Benutzernamen — dieselbe Regel wie beim Anlegen.</summary>
    public static string NutzerIdVon(string benutzername) => Tagebuch.NutzerId(benutzername);
}
