using System.Security.Cryptography;
using System.Text;

namespace AccessControl.Domain.Users;

// Token de un solo uso con vencimiento. Guarda solo el hash, nunca el token real.
public sealed record OneTimeToken
{
    public string TokenHash { get; private init; } = default!;
    public DateTime IssuedAt { get; private init; }
    public DateTime ExpiresAt { get; private init; }
    public DateTime? UsedAt { get; private init; }

    private OneTimeToken() { } // Constructo para EF Core

    public static OneTimeToken Issue(string tokenHash, DateTime utcNow, TimeSpan lifetime)
        => new()
        {
            TokenHash = tokenHash,
            IssuedAt = utcNow,
            ExpiresAt = utcNow.Add(lifetime)
        };

    // ¿Este hash coincide, no se usó y no ha vencido?
    public bool IsValidFor(string tokenHash, DateTime utcNow)
    {
        if (UsedAt is not null || utcNow >= ExpiresAt)
            return false;

        // Comparación en tiempo constante: no revela cuántos caracteres coinciden.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(TokenHash),
            Encoding.UTF8.GetBytes(tokenHash));
    }

    public OneTimeToken MarkAsUsed(DateTime utcNow) => this with { UsedAt = utcNow };
}