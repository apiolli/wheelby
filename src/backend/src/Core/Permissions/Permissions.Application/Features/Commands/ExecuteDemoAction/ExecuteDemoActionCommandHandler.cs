using AccessControl.Contracts;
using MediatR;
using Permissions.Contracts;
using SharedKernel.Exceptions;

namespace Permissions.Application.Features.ExecuteDemoAction;

internal sealed class ExecuteDemoActionCommandHandler(
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser) : IRequestHandler<ExecuteDemoActionCommand, DemoActionResult>
{
    public async Task<DemoActionResult> Handle(
        ExecuteDemoActionCommand request,
        CancellationToken cancellationToken)
    {
        var hasPermission = await permissionChecker.HasPermissionAsync(
            currentUser.Id,
            PermissionCodes.DemoAction,
            cancellationToken);

        if (!hasPermission)
            throw new ForbiddenException("No tienes el permiso requerido para esta operación.");

        return new DemoActionResult("Acción de demostración ejecutada.");
    }
}
