using AccessControl.Application.Abstractions;
using MediatR;
using SharedKernel.Exceptions;

namespace AccessControl.Application.Features.ChangeRole;

internal sealed class ChangeUserRoleCommandHandler(IUserRepository users)
    : IRequestHandler<ChangeUserRoleCommand, Unit>
{
    public async Task<Unit> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("El usuario no existe.");

        RoleParser.TryParse(request.Role, out var role);
        user.ChangeRole(role);

        await users.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}