namespace Weekplan.Core.Tagebuch.Contracts;

/// <summary>
/// Die Daten, die einem Nutzer gehoeren. Profil und Woche liegen getrennt, weil
/// sie in verschiedenen Rhythmen geschrieben werden — Gewicht taeglich, Plan
/// sonntags — und weil die Haken spaeter einzeln nachgetragen werden muessen.
/// </summary>
public interface ITagebuch
{
    Task<Konto?> KontoAsync(string benutzername, CancellationToken ct = default);

    /// <summary>Legt ein Konto an. Wirft, wenn der Benutzername schon vergeben ist.</summary>
    Task KontoAnlegenAsync(Konto konto, CancellationToken ct = default);

    /// <summary>Der Profilstand, oder <see cref="ProfilStand.Leer"/> bei einem frischen Konto.</summary>
    Task<ProfilStand> ProfilAsync(string nutzerId, CancellationToken ct = default);

    Task ProfilSpeichernAsync(string nutzerId, ProfilStand profil, CancellationToken ct = default);

    /// <summary>Der Wochenstand, oder <see cref="WochenStand.Leer"/> bei einem frischen Konto.</summary>
    Task<WochenStand> WocheAsync(string nutzerId, CancellationToken ct = default);

    Task WocheSpeichernAsync(string nutzerId, WochenStand woche, CancellationToken ct = default);
}
