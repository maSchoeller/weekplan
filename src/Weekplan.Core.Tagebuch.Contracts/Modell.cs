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

/// <summary>Der Wochenplan samt Haken. Plan ist Tag → Mahlzeit → Gerichte.</summary>
public sealed record WochenStand(
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>> Plan,
    string RefeedTag,
    int Rotation,
    IReadOnlyDictionary<string, bool> HakenWoche,
    IReadOnlyDictionary<string, bool> HakenGrundstock)
{
    public static WochenStand Leer { get; } = new(
        Plan: new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<PlanEintrag>>>(),
        RefeedTag: "Sa", Rotation: 0,
        HakenWoche: new Dictionary<string, bool>(),
        HakenGrundstock: new Dictionary<string, bool>());
}
