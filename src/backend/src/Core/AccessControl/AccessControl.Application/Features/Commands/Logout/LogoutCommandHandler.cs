using AccessControl.Application.Abstractions;
using MediatR;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.Logout;

internal sealed class LogoutCommandHandler(
    ISessionRepository sessions,
    ICurrentSession currentSession,
    IClock clock) : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(currentSession.SessionId, cancellationToken);

        // Cerrar una sesión inexistente o ya cerrada deja todo como estaba
        if (session is null)
            return Unit.Value;

        session.Revoke(clock.UtcNow);
        await sessions.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}