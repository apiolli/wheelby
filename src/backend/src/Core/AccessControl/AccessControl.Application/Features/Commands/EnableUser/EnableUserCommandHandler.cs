using AccessControl.Application.Abstractions;
using MediatR;
using SharedKernel.Exceptions;

namespace AccessControl.Application.Features.EnableUser;

internal sealed class EnableUserCommandHandler(IUserRepository users)
    : IRequestHandler<EnableUserCommand, Unit>
{
    public async Task<Unit> Handle(EnableUserCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("El usuario no existe.");

        // Reactivar no salta la confirmación del correo: si nunca activó, sigue sin poder entrar.
        user.Enable();

        await users.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}