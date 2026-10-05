using AccessControl.Application.Abstractions;
using AccessControl.Domain.Sessions;

namespace AccessControl.Application.Common;

// Crea una sesión nueva y emite su token. Lo usan el inicio de sesión y la renovación.
internal static class SessionStart
{
    public static async Task<IssuedToken> StartAsync(
        ISessionRepository sessions,
        ITokenIssuer tokenIssuer,
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var session = Session.Start(userId, utcNow, tokenIssuer.Lifetime);
        await sessions.AddAsync(session, cancellationToken);

        await sessions.SaveChangesAsync(cancellationToken);

        return tokenIssuer.Issue(userId, session.Id, session.CreatedAt, session.ExpiresAt);
    }
}