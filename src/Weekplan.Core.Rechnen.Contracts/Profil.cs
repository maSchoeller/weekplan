namespace Weekplan.Core.Rechnen.Contracts;

/// <summary>Die Werte, aus denen der Grundumsatz folgt.</summary>
/// <param name="GewichtKg">Aktuelles Körpergewicht in Kilogramm.</param>
/// <param name="GroesseCm">Körpergröße in Zentimetern.</param>
/// <param name="Alter">Alter in vollen Jahren.</param>
public sealed record Profil(double GewichtKg, double GroesseCm, int Alter);
