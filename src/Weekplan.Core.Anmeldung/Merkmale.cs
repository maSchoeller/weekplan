using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Weekplan.Core.Anmeldung.Contracts;

namespace Weekplan.Core.Anmeldung;

/// <summary>
/// Signiertes Merkmal (JWT, HS256). Kein Cookie: Client und Server liegen auf
/// verschiedenen Herkuenften, und ein Cookie muesste dafuer SameSite=None sein —
/// genau solche schraenken Browser zunehmend ein.
/// </summary>
internal sealed class Merkmale(SymmetricSecurityKey schluessel) : IMerkmale
{
    private const string Aussteller = "weekplan";
    private readonly JsonWebTokenHandler _handler = new();

    public string Erzeugen(string nutzerId) => _handler.CreateToken(new SecurityTokenDescriptor
    {
        Issuer = Aussteller,
        Audience = Aussteller,
        Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, nutzerId)]),
        // Kein praktischer Ablauf — so hat der Nutzer es gewaehlt.
        Expires = DateTime.UtcNow.AddYears(100),
        SigningCredentials = new SigningCredentials(schluessel, SecurityAlgorithms.HmacSha256)
    });

    public async ValueTask<string?> NutzerAusAsync(string? merkmal)
    {
        if (string.IsNullOrWhiteSpace(merkmal)) return null;

        var ergebnis = await _handler.ValidateTokenAsync(merkmal, new TokenValidationParameters
        {
            ValidIssuer = Aussteller,
            ValidAudience = Aussteller,
            IssuerSigningKey = schluessel,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        });

        return ergebnis.IsValid
            ? ergebnis.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
              ?? ergebnis.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;
    }
}
