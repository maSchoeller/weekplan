using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Rechnen.Contracts;

namespace Weekplan.Core.Rechnen;

/// <summary>Einziger Eingang in den Slice — die Umsetzung bleibt <c>internal</c>.</summary>
public static class RechnenServiceCollectionExtensions
{
    public static IServiceCollection AddRechnen(this IServiceCollection services)
        => services.AddSingleton<IGrundumsatzRechner, MifflinStJeorRechner>();
}
