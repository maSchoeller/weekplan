using Weekplan.Core.Rechnen.Contracts;

namespace Weekplan.Core.Rechnen;

/// <summary>
/// Mifflin-St Jeor (männlich). Herleitung und Begründung: docs/plan.md.
/// </summary>
internal sealed class MifflinStJeorRechner : IGrundumsatzRechner
{
    public double Berechnen(Profil profil)
        => 10 * profil.GewichtKg + 6.25 * profil.GroesseCm - 5 * profil.Alter + 5;
}
