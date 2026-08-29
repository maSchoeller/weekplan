using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Core.Wochenplanung.Contracts;

namespace Weekplan.Core.Tests;

/// <summary>
/// Die erlaubten Kategorien stehen zweimal: bei den Stammdaten, weil die
/// Pruefung eines geschriebenen Rezepts sie braucht, und bei der Wochenplanung,
/// weil dort Beschriftung und Kalorienanteil je Mahlzeit haengen. Ein Zugriff
/// des einen Slice auf den anderen waere ein Ringschluss — also halten sie hier
/// zusammen, und zwar in Reihenfolge: die Rezeptseite zeigt die Mahlzeiten in
/// genau dieser Folge.
/// </summary>
public class KategorienTests
{
    [Fact]
    public void Erlaubte_Kategorien_und_Mahlzeiten_der_Woche_fuehren_dieselben_Schluessel()
        => Assert.Equal(Kategorien.Erlaubt, Woche.Mahlzeiten.Select(m => m.Schluessel));
}
