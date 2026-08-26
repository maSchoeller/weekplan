namespace Weekplan.Core.Anmeldung.Contracts;

/// <summary>
/// Passwoerter werden nie im Klartext abgelegt. Dieser Slice kennt keine
/// Datenbank — er bekommt den gespeicherten Hash gereicht und urteilt.
/// </summary>
public interface IPasswoerter
{
    /// <summary>Erzeugt den abzulegenden Hash. Zweimal derselbe Ruf ergibt zwei verschiedene Hashes.</summary>
    string Hashen(string passwort);

    /// <summary>Prueft ein Passwort gegen einen abgelegten Hash.</summary>
    bool Stimmt(string hash, string passwort);
}
