using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Rechnen;
using Weekplan.Core.Rechnen.Contracts;

namespace Weekplan.Core.Tests;

public class GrundumsatzTests
{
    private static IGrundumsatzRechner Rechner() =>
        new ServiceCollection().AddRechnen().BuildServiceProvider()
            .GetRequiredService<IGrundumsatzRechner>();

    // Mifflin-St Jeor (männlich), wie in docs/plan.md festgehalten:
    // 10 × Gewicht + 6,25 × Größe − 5 × Alter + 5
    [Theory]
    [InlineData(80, 180, 35, 1755)]   // 800 + 1125 − 175 + 5
    [InlineData(60, 165, 50, 1386.25)]   // 600 + 1031,25 − 250 + 5
    public void Berechnet_Grundumsatz_nach_Mifflin_St_Jeor(
        double gewicht, double groesse, int alter, double erwartet)
    {
        var ergebnis = Rechner().Berechnen(new Profil(gewicht, groesse, alter));

        Assert.Equal(erwartet, ergebnis, precision: 4);
    }
}
