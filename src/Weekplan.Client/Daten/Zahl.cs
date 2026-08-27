using System.Globalization;

namespace Weekplan.Client.Daten;

/// <summary>
/// Zahlen so, wie sie im Deutschen gelesen werden — an einer Stelle, damit
/// nicht jede Seite ihr eigenes Format erfindet.
/// </summary>
public static class Zahl
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Kalorien mit Tausenderpunkt: 1.944</summary>
    public static string Kcal(double wert) => wert.ToString("N0", De);

    /// <summary>Kilogramm mit einer Nachkommastelle, ohne unnoetige Null: 5 statt 5,0</summary>
    public static string Kg(double wert)
        => Math.Abs(wert - Math.Round(wert)) < 0.05
            ? Math.Round(wert).ToString("N0", De)
            : wert.ToString("N1", De);

    /// <summary>Zwei Nachkommastellen fuer das Tempo: 0,55</summary>
    public static string Tempo(double wert) => wert.ToString("N2", De);

    /// <summary>
    /// Mengen aus der Einkaufsliste: unter 1000 g in Gramm, darueber in
    /// Kilogramm — 1.250 g liest sich schlechter als 1,25 kg.
    /// </summary>
    public static string Gramm(double g)
    {
        var gerundet = Math.Round(g);
        return gerundet >= 1000
            ? (gerundet / 1000).ToString("N2", De) + " kg"
            : gerundet.ToString("N0", De) + " g";
    }

    /// <summary>Liest eine Eingabe, die auch ein Komma enthalten darf.</summary>
    public static double? Aus(string? roh)
    {
        if (string.IsNullOrWhiteSpace(roh)) return null;

        var sauber = roh.Trim().Replace(',', '.');
        return double.TryParse(sauber, NumberStyles.Float, CultureInfo.InvariantCulture, out var wert)
            ? wert
            : null;
    }

    /// <summary>Fuer Eingabefelder: der Wert mit Komma, oder leer.</summary>
    public static string Feld(double? wert)
        => wert is null ? string.Empty : wert.Value.ToString("0.##", De);
}
