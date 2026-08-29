namespace Weekplan.Core.Stammdaten;

/// <summary>
/// Die Dokumentnamen an einer Stelle. Zwei Partitionen: die Rezepte, weil sie
/// einzeln geschrieben und gemeinsam gelesen werden, und die drei Listen, die
/// je ein Dokument sind.
/// </summary>
internal static class Namen
{
    public const string Rezept = "rezept";
    public const string Liste = "liste";

    public const string Training = "training";
    public const string Grundstock = "grundstock";
    public const string Abteilungen = "abteilungen";
}
