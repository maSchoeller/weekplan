using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Tagebuch;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Der Vertrag aus <see cref="TagebuchVertrag"/>, gegen echte Dateien geprueft —
/// nicht gegen eine Attrappe: die Ablage ist genau der Teil, an dem ein Fehler
/// weh taete, und ein Attrappentest wuerde ihn nicht sehen.
/// </summary>
public sealed class TagebuchInDateienTests : TagebuchVertrag, IDisposable
{
    private readonly string _ordner = Path.Combine(
        Path.GetTempPath(), "weekplan-tests", Guid.NewGuid().ToString("n"));

    protected override ITagebuch Tagebuch() =>
        new ServiceCollection().AddTagebuchInDateien(_ordner).BuildServiceProvider()
            .GetRequiredService<ITagebuch>();

    public void Dispose()
    {
        if (Directory.Exists(_ordner)) Directory.Delete(_ordner, recursive: true);
    }

    /// <summary>
    /// Nur hier, nicht im Vertrag: dass ein Pfadtrenner nicht aus dem Ordner
    /// ausbricht, ist eine Eigenschaft der Dateiablage. Cosmos kennt keine
    /// Ordner — dort weist derselbe Name aus einem anderen Grund ab.
    /// </summary>
    [Fact]
    public async Task Ein_Nutzername_mit_Pfadtrennern_bricht_nicht_aus_dem_Ordner_aus()
    {
        var t = Tagebuch();

        await Assert.ThrowsAsync<ArgumentException>(
            () => t.ProfilSpeichernAsync("../../ausbruch", ProfilStand.Leer));

        Assert.False(Directory.Exists(Path.Combine(_ordner, "..", "..", "ausbruch")));
    }
}
