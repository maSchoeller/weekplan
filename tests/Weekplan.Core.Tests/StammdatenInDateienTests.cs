using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Stammdaten;
using Weekplan.Core.Stammdaten.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Der Vertrag aus <see cref="StammdatenVertrag"/>, gegen echte Dateien
/// geprueft. Das ist die Ablage, die <c>run-local.ps1</c> und der Smoketest
/// benutzen — sie muss so genau stimmen wie die in Azure.
/// </summary>
public sealed class StammdatenInDateienTests : StammdatenVertrag, IDisposable
{
    private readonly string _ordner = Path.Combine(
        Path.GetTempPath(), "weekplan-tests", Guid.NewGuid().ToString("n"));

    protected override IStammdaten Quelle() => In(_ordner);

    private static IStammdaten In(string ordner) =>
        new ServiceCollection().AddStammdatenInDateien(ordner).BuildServiceProvider()
            .GetRequiredService<IStammdaten>();

    public void Dispose()
    {
        if (Directory.Exists(_ordner)) Directory.Delete(_ordner, recursive: true);
    }

    /// <summary>
    /// Nur hier, nicht im Vertrag: eine leere Ablage laesst sich nur dort
    /// herstellen, wo der Test den Speicher selbst besitzt. In Cosmos gaebe es
    /// dafuer keinen gefahrlosen Weg.
    ///
    /// <para>
    /// Ohne Abteilungen sortiert die Einkaufsliste nicht, ohne Phasen rechnet
    /// die App nicht. Ein leerer Satz waere also keine leere App, sondern eine
    /// falsche — deshalb ein Fehler und keine Vorgabewerte.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Eine_unbefuellte_Ablage_meldet_einen_Fehler_statt_leerer_Daten()
    {
        var leer = Path.Combine(Path.GetTempPath(), "weekplan-tests", Guid.NewGuid().ToString("n"));

        await Assert.ThrowsAsync<StammdatenFehlenException>(() => In(leer).AllesAsync());
    }

    /// <summary>
    /// Eine Rezeptkennung wird zum Dateinamen. Ohne Pruefung waere
    /// „../../ausbruch" ein Schreibzugriff ausserhalb des Ordners — dieselbe
    /// Gefahr wie beim Tagebuch, dieselbe Antwort.
    /// </summary>
    [Fact]
    public async Task Eine_Kennung_mit_Pfadtrennern_bricht_nicht_aus_dem_Ordner_aus()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Quelle().RezeptAsync("../../ausbruch"));
    }
}
