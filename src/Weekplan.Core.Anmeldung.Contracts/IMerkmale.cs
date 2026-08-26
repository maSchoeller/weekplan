namespace Weekplan.Core.Anmeldung.Contracts;

/// <summary>
/// Das Merkmal, mit dem ein Geraet dauerhaft angemeldet bleibt. Bewusst ohne
/// praktischen Ablauf: der Nutzer will sich einmal pro Geraet ausweisen und nie
/// wieder. Ein Wechsel des Signaturschluessels wirft alle Geraete hinaus.
/// </summary>
public interface IMerkmale
{
    string Erzeugen(string nutzerId);

    /// <summary>Der Nutzer hinter einem Merkmal, oder <c>null</c> wenn es nicht gilt.</summary>
    ValueTask<string?> NutzerAusAsync(string? merkmal);
}
