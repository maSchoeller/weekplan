namespace Weekplan.Core.Rechnen.Contracts;

/// <summary>Grundumsatz (Ruheenergiebedarf) in Kilokalorien pro Tag.</summary>
public interface IGrundumsatzRechner
{
    double Berechnen(Profil profil);
}
