using AccessControl.Contracts;
using MediatR;
using Permissions.Application.Abstractions;
using Permissions.Domain.PermissionGrants;
using Permissions.Domain.PermissionRequests.Exceptions;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;

namespace Permissions.Application.Features.ApprovePermissionRequest;

internal sealed class ApprovePermissionRequestCommandHandler(
    IPermissionRequestRepository requests,
    IPermissionGrantRepository grants,
    IPermissionsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<ApprovePermissionRequestCommand, Unit>
{
    public async Task<Unit> Handle(
        ApprovePermissionRequestCommand request,
        CancellationToken cancellationToken)
    {
        // Orden de comprobaciones: 404, 403 (propia) y 409 (estado).
        var permissionRequest = await requests.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException("La solicitud no existe.");

        if (permissionRequest.RequesterId == currentUser.Id)
            throw new CannotResolveOwnPermissionRequestException(
                "No puedes resolver tu propia solicitud de permiso.");

        var utcNow = clock.UtcNow;

        // Aprobar y aplicar, y crear la concesión, todo en la misma operación.
        permissionRequest.Approve(currentUser.Id, utcNow);
        permissionRequest.MarkApplied(utcNow);

        var grant = PermissionGrant.Create(
            permissionRequest.RequesterId,
            permissionRequest.PermissionCode,
            permissionRequest.Id,
            utcNow);

        await grants.AddAsync(grant, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
