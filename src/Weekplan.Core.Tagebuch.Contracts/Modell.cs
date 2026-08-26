using Weekplan.Core.Rechnen.Contracts;

namespace Weekplan.Core.Tagebuch.Contracts;

/// <summary>Ein Konto. Angelegt wird es nur vom Werkzeug — es gibt keine Registrierung.</summary>
public sealed record Konto(string NutzerId, string Benutzername, string PasswortHash);

/// <summary>Ein Gericht auf einem Mahlzeitenplatz, mit seiner Portionszahl.</summary>
public sealed record PlanEintrag(string RezeptId, int Portionen);

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
