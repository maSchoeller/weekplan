using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Weekplan.Core.Anmeldung.Contracts;

namespace Weekplan.Core.Anmeldung;

/// <summary>Einziger Eingang in den Slice — die Umsetzungen bleiben <c>internal</c>.</summary>
public static class AnmeldungServiceCollectionExtensions
{
    /// <param name="signaturSchluessel">
    /// Mindestens 32 Zeichen (HS256 verlangt 256 Bit). Ein Wechsel wirft alle
    /// angemeldeten Geraete hinaus — das ist das eingebaute „ueberall abmelden".
    /// </param>
    public static IServiceCollection AddAnmeldung(this IServiceCollection services, string signaturSchluessel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signaturSchluessel);
        if (Encoding.UTF8.GetByteCount(signaturSchluessel) < 32)
        {
            throw new ArgumentException(
                "Der Signaturschluessel braucht mindestens 32 Zeichen.", nameof(signaturSchluessel));
        }

        var schluessel = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signaturSchluessel));

        services.AddSingleton<IPasswoerter, Passwoerter>();
        services.AddSingleton<IMerkmale>(_ => new Merkmale(schluessel));
        return services;
    }
}
