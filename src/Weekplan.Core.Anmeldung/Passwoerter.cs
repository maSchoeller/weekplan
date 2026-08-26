using Microsoft.AspNetCore.Identity;
using Weekplan.Core.Anmeldung.Contracts;

namespace Weekplan.Core.Anmeldung;

/// <summary>
/// Duenne Huelle um <see cref="PasswordHasher{TUser}"/> — die einzelne Klasse aus
/// ASP.NET Core Identity, nicht das ganze Identity-System. Sie salzt selbst und
/// bringt das aktuelle Iterationsverfahren mit.
/// </summary>
internal sealed class Passwoerter : IPasswoerter
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object Niemand = new();

    public string Hashen(string passwort) => _hasher.HashPassword(Niemand, passwort);

    public bool Stimmt(string hash, string passwort)
    {
        try
        {
            return _hasher.VerifyHashedPassword(Niemand, hash, passwort)
                is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // Ein unlesbarer Hash ist kein Absturz, sondern schlicht kein Treffer.
            return false;
        }
    }
}
