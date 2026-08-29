using Weekplan.Core.Rechnen.Contracts;
using Weekplan.Core.Stammdaten.Contracts;
using Weekplan.Core.Tagebuch.Contracts;

namespace Weekplan.Core.Wochenplanung.Contracts;

public interface IWochenplanung
{
    /// <summary>Alle geplanten Portionen zu einer Einkaufsliste summiert, nach Abteilung sortiert.</summary>
    Einkaufsliste Einkaufsliste(WochenStand woche, IReadOnlyList<Rezept> rezepte, IReadOnlyList<string> abteilungen);

    /// <summary>
    /// Fuellt die Woche so, dass jeder Tag nah an seinem Kalorienziel landet und
    /// das Protein erreicht wird. Rotiert gegenueber dem letzten Aufruf.
    /// </summary>
    WochenStand AutomatischFuellen(WochenStand woche, IReadOnlyList<Rezept> rezepte, Bilanz bilanz);

    /// <summary>Kalorien und Protein eines Tages nach Plan.</summary>
    (int Kcal, int Protein) Tagessumme(WochenStand woche, string tag, IReadOnlyList<Rezept> rezepte);
}
