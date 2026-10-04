using AccessControl.Application.Abstractions;
using AccessControl.Domain.Users.Exceptions;
using MediatR;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.Activate;

internal sealed class ActivateAccountCommandHandler(
    IUserRepository users,
    IOneTimeTokenGenerator tokenGenerator,
    IClock clock) : IRequestHandler<ActivateAccountCommand, Unit>
{
    public async Task<Unit> Handle(ActivateAccountCommand request, CancellationToken cancellationToken)
    {
        // Usuario inexistente y token incorrecto dan exactamente el mismo rechazo.
        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidActivationTokenException();

        user.Activate(tokenGenerator.Hash(request.Token), clock.UtcNow);
        await users.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}