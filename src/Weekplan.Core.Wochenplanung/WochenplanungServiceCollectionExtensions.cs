using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Wochenplanung.Contracts;

namespace Weekplan.Core.Wochenplanung;

/// <summary>Einziger Eingang in den Slice — die Umsetzung bleibt <c>internal</c>.</summary>
public static class WochenplanungServiceCollectionExtensions
{
    public static IServiceCollection AddWochenplanung(this IServiceCollection services)
        => services.AddSingleton<IWochenplanung, Wochenplanung>();
}
