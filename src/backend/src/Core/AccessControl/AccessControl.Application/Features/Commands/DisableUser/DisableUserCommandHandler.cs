using AccessControl.Application.Abstractions;
using AccessControl.Contracts;
using MediatR;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;

namespace AccessControl.Application.Features.DisableUser;

internal sealed class DisableUserCommandHandler(
    IUserRepository users,
    ISessionRepository sessions,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<DisableUserCommand, Unit>
{
    public async Task<Unit> Handle(DisableUserCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("El usuario no existe");

        var utcNow = clock.UtcNow;

        // El dominio impide que un administrador se desactive a sí mismo
        user.Disable(currentUser.Id, utcNow);

        // Se revocan todas sus sesiones
        foreach (var session in await sessions.GetActiveByUserIdAsync(user.Id, utcNow, cancellationToken))
            session.Revoke(utcNow);
            
        await users.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}