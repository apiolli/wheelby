using AccessControl.Application.Abstractions;

namespace AccessControl.Application.Common;

// Revoca todas las sesiones vigentes del usuario. No guarda: el handler guarda una sola vez.
internal static class SessionRevocation
{
    public static async Task RevokeAllAsync(
        ISessionRepository sessions, Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        foreach (var session in await sessions.GetActiveByUserIdAsync(userId, utcNow, cancellationToken))
            session.Revoke(utcNow);
    }
}
