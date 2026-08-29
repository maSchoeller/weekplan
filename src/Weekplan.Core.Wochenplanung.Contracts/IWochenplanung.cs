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

    /// <summary>
    /// Welche Regel dieser Rezeptpool nicht bedienen kann. Jede Vorauswahl beim
    /// Fuellen hat einen Rueckfall auf die volle Auswahl — der ist noetig, damit
    /// kein Tag leer bleibt, aber er ist still: die Woche saehe regelwidrig aus
    /// und wuesste nicht zu sagen warum. Diese Saetze sagen es.
    /// </summary>
    IReadOnlyList<string> Fuellhinweise(IReadOnlyList<Rezept> rezepte);

    /// <summary>Kalorien und Protein eines Tages nach Plan.</summary>
    (int Kcal, int Protein) Tagessumme(WochenStand woche, string tag, IReadOnlyList<Rezept> rezepte);
}
