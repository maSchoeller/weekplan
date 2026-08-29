using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Core.Stammdaten;

/// <summary>Einziger Eingang in den Slice — die Umsetzungen bleiben <c>internal</c>.</summary>
public static class StammdatenServiceCollectionExtensions
{
    /// <summary>Ablage als JSON-Dateien unter <paramref name="ordner"/>: lokal und im Smoketest.</summary>
    public static IServiceCollection AddStammdatenInDateien(this IServiceCollection services, string ordner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ordner);
        services.AddSingleton<IAblage>(_ => new DateiAblage(ordner));
        services.AddSingleton<IStammdaten, Stammdatendienst>();
        return services;
    }

    /// <summary>
    /// Ablage in einem Cosmos-Container: in Azure. Den Container muss es schon
    /// geben — angelegt wird er beim Ausrollen, nicht beim Starten.
    /// </summary>
    public static IServiceCollection AddStammdatenInCosmos(
        this IServiceCollection services, string verbindung, string datenbank, string behaelter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verbindung);
        ArgumentException.ThrowIfNullOrWhiteSpace(datenbank);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaelter);
        services.AddSingleton<IAblage>(_ => new CosmosAblage(verbindung, datenbank, behaelter));
        services.AddSingleton<IStammdaten, Stammdatendienst>();
        return services;
    }
}
