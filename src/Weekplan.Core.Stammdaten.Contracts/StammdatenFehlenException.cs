namespace Weekplan.Core.Stammdaten.Contracts;

/// <summary>
/// Die Ablage ist nicht befuellt. Kein Fehler des Aufrufers, sondern ein
/// fehlender Betriebsschritt: das Erstbefuellungswerkzeug lief nie.
/// </summary>
public sealed class StammdatenFehlenException(string meldung) : Exception(meldung)
{
    public static StammdatenFehlenException Fuer(string dokument) => new(
        $"Das Dokument {dokument} fehlt in der Ablage. Die Stammdaten sind nicht befuellt — "
        + "tools/Weekplan.Stammdaten muss einmal gelaufen sein.");
}
