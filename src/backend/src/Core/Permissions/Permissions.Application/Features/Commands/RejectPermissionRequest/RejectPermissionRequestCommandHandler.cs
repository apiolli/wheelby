using AccessControl.Contracts;
using MediatR;
using Permissions.Application.Abstractions;
using Permissions.Domain.PermissionRequests.Exceptions;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;

namespace Permissions.Application.Features.RejectPermissionRequest;

internal sealed class RejectPermissionRequestCommandHandler(
    IPermissionRequestRepository requests,
    IPermissionsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<RejectPermissionRequestCommand, Unit>
{
    public async Task<Unit> Handle(
        RejectPermissionRequestCommand request,
        CancellationToken cancellationToken)
    {
        // Orden de comprobaciones: 404, 403 (propia) y 409 (estado).
        var permissionRequest = await requests.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException("La solicitud no existe.");

        if (permissionRequest.RequesterId == currentUser.Id)
            throw new CannotResolveOwnPermissionRequestException(
                "No puedes resolver tu propia solicitud de permiso.");

        // El motivo obligatorio lo exige el dominio (400) y no el validador,
        // para no saltarse el orden de comprobaciones.
        permissionRequest.Reject(currentUser.Id, clock.UtcNow, request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
