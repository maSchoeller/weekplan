using Weekplan.Core.Rechnen.Contracts;

namespace Weekplan.Core.Tagebuch.Contracts;

/// <summary>Ein Konto. Angelegt wird es nur vom Werkzeug — es gibt keine Registrierung.</summary>
public sealed record Konto(string NutzerId, string Benutzername, string PasswortHash);

/// <summary>
/// Ein Gericht auf einem Mahlzeitenplatz, mit seiner Portionszahl — und mit den
/// Naehrwerten, die das Rezept beim Planen hatte.
///
/// <para>
/// Die beiden gemerkten Zahlen sind der ganze Trick hinter „nichts verschiebt
/// sich still": Rezepte lassen sich seit dem Lauf 2026-08-28 jederzeit aendern,
/// und eine abgehakte Woche wuerde sich sonst lautlos umrechnen. Gerechnet wird
/// weiterhin mit dem **aktuellen** Rezept — die gemerkten Werte dienen allein
/// dem Hinweis. Beide sind <c>null</c>, wenn der Eintrag aus einer Zeit vor
/// dieser Aenderung stammt; dann gibt es nichts zu vergleichen und auch keinen
/// Hinweis.
/// </para>
/// </summary>
public sealed record PlanEintrag(
    string RezeptId, int Portionen, int? KcalBeimPlanen = null, int? ProteinBeimPlanen = null);

/// <summary>Alles, was der Nutzer ueber sich eingetragen hat.</summary>
public sealed record ProfilStand(
    double GewichtKg,
    double ZielKg,
    double GroesseCm,
    int Alter,
    DateOnly? Zieltermin,
    double ProteinFaktor,
    string PhaseId,
    double? TempoKgProWoche,
    IReadOnlyList<Gewichtseintrag> Verlauf)
{
    /// <summary>Neutrale Startwerte — bewusst nicht die Werte eines Nutzers.</summary>
    public static ProfilStand Leer { get; } = new(
        GewichtKg: 80, ZielKg: 75, GroesseCm: 180, Alter: 35, Zieltermin: null,
        ProteinFaktor: 2.0, PhaseId: "p1", TempoKgProWoche: null, Verlauf: []);
}

/// <summary>
/// Der Wochenplan samt Haken. Plan ist Tag → Mahlzeit → Gerichte.
///
/// <para>
/// <paramref name="GaesteTag"/> und <paramref name="GaesteMahlzeit"/> tragen die
/// <b>zusaetzlichen Esser</b> — Tag → Zahl und „Tag|Mahlzeit" → Zahl. Sie sind
/// die Antwort auf die Doppeldeutigkeit der Portionszahl: <see cref="PlanEintrag.Portionen"/>
/// bleibt allein die eigene Portion und traegt die Bilanz, die Gaestezahl traegt
/// Einkauf und Kochmenge. Was die Bilanz rechnet, kennt die zweite Zahl gar
/// nicht und kann sie darum nicht verfaelschen.
/// </para>
///
/// <para>
/// <b>Zwei Sammlungen statt einer</b> mit gemischten Schluesseln, weil eine
/// gesetzte 0 an der Mahlzeit („die Gaeste fruehstuecken nicht mit") etwas
/// anderes ist als keine Angabe. Beide sind <c>null</c>, wenn das gespeicherte
/// Dokument aus der Zeit vor diesem Merkmal stammt — <see cref="Gaeste"/>
/// behandelt das wie leer.
/// </para>
/// </summary>
public sealed record WochenStand(
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>> Plan,
    string RefeedTag,
    int Rotation,
    IReadOnlyDictionary<string, bool> HakenWoche,
    IReadOnlyDictionary<string, bool> HakenGrundstock,
    IReadOnlyDictionary<string, int>? GaesteTag = null,
    IReadOnlyDictionary<string, int>? GaesteMahlzeit = null)
{
    public static WochenStand Leer { get; } = new(
        Plan: new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>(),
        RefeedTag: "Sa", Rotation: 0,
        HakenWoche: new Dictionary<string, bool>(),
        HakenGrundstock: new Dictionary<string, bool>());

    /// <summary>Der Schluessel einer Mahlzeit-Ausnahme.</summary>
    public static string Mahlzeitschluessel(string tag, string mahlzeit) => $"{tag}|{mahlzeit}";

    /// <summary>
    /// Wie viele zusaetzlich mitessen: die Ausnahme der Mahlzeit, sonst die Zahl
    /// des Tages, sonst niemand.
    /// </summary>
    public int Gaeste(string tag, string mahlzeit)
    {
        if (GaesteMahlzeit is not null
            && GaesteMahlzeit.TryGetValue(Mahlzeitschluessel(tag, mahlzeit), out var ausnahme))
        {
            return ausnahme;
        }

        return GaesteTag is not null && GaesteTag.TryGetValue(tag, out var amTag) ? amTag : 0;
    }

    /// <summary>Die Zahl am Tag selbst, ohne die Ausnahmen der Mahlzeiten.</summary>
    public int GaesteAmTag(string tag)
        => GaesteTag is not null && GaesteTag.TryGetValue(tag, out var zahl) ? zahl : 0;

    /// <summary>Ob die Mahlzeit eine eigene Zahl traegt statt der des Tages.</summary>
    public bool HatEigeneGaeste(string tag, string mahlzeit)
        => GaesteMahlzeit?.ContainsKey(Mahlzeitschluessel(tag, mahlzeit)) == true;
}
