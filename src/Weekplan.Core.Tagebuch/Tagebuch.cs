using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Core.Tagebuch;

internal sealed class Tagebuch(IAblage ablage) : ITagebuch
{
    private const string Profil = "profil";
    private const string Woche = "woche";
    private const string KontoName = "konto";

    public async Task<Konto?> KontoAsync(string benutzername, CancellationToken ct = default)
        => await ablage.LesenAsync<Konto>(NutzerId(benutzername), KontoName, ct);

    public async Task KontoAnlegenAsync(Konto konto, CancellationToken ct = default)
    {
        if (await KontoAsync(konto.Benutzername, ct) is not null)
        {
            throw new InvalidOperationException(
                $"Der Benutzername '{konto.Benutzername}' ist bereits vergeben.");
        }
        await ablage.SchreibenAsync(konto.NutzerId, KontoName, konto, ct);
    }

    public async Task<ProfilStand> ProfilAsync(string nutzerId, CancellationToken ct = default)
        => await ablage.LesenAsync<ProfilStand>(nutzerId, Profil, ct) ?? ProfilStand.Leer;

    public Task ProfilSpeichernAsync(string nutzerId, ProfilStand profil, CancellationToken ct = default)
        => ablage.SchreibenAsync(nutzerId, Profil, profil, ct);

    public async Task<WochenStand> WocheAsync(string nutzerId, CancellationToken ct = default)
        => await ablage.LesenAsync<WochenStand>(nutzerId, Woche, ct) ?? WochenStand.Leer;

    public Task WocheSpeichernAsync(string nutzerId, WochenStand woche, CancellationToken ct = default)
        => ablage.SchreibenAsync(nutzerId, Woche, woche, ct);

    /// <summary>Die Nutzerkennung ist der kleingeschriebene Benutzername.</summary>
    internal static string NutzerId(string benutzername) => benutzername.Trim().ToLowerInvariant();
}
