using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Application.Features.Login;
using AccessControl.Contracts;
using AccessControl.Domain.Sessions.Exceptions;
using MediatR;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.Refresh;

internal sealed class RefreshSessionCommandHandler(
    ISessionRepository sessions,
    ICurrentSession currentSession,
    ICurrentUser currentUser,
    ITokenIssuer tokenIssuer,
    IClock clock) : IRequestHandler<RefreshSessionCommand, LoginResultDTO>
{
    public async Task<LoginResultDTO> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var utcNow = clock.UtcNow;

        var current = await sessions.GetByIdAsync(currentSession.SessionId, cancellationToken);
        if (current is null || !current.IsValid(utcNow))
            throw new InvalidSessionException();

        // Rotación: la sesión anterior deja de servir en cuanto existe la nueva.
        current.Revoke(utcNow);

        // Un solo guardado: se revoca la anterior y se crea la nueva, o no ocurre ninguna de las dos.
        var token = await SessionStart.StartAsync(
            sessions, tokenIssuer, currentUser.Id, utcNow, cancellationToken);

        return new LoginResultDTO(token.Value, token.ExpiresAt);
    }
}