using AccessControl.Contracts;
using MediatR;
using Permissions.Application.Abstractions;
using Permissions.Domain.PermissionRequests;
using Shared.Application.Abstractions;
using SharedKernel.Exceptions;

namespace Permissions.Application.Features.CreatePermissionRequest;

internal sealed class CreatePermissionRequestCommandHandler(
    IPermissionRequestRepository requests,
    IPermissionGrantRepository grants,
    IPermissionCatalog catalog,
    IPermissionsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<CreatePermissionRequestCommand, Guid>
{
    public async Task<Guid> Handle(CreatePermissionRequestCommand request, CancellationToken cancellationToken)
    {
        // El catálogo es la fuente de verdad: un código que no existe no se puede solicitar.
        if (!catalog.Exists(request.PermissionCode))
            throw new BadRequestException($"El permiso '{request.PermissionCode}' no existe en el catálogo.");

        if (await requests.ExistsPendingAsync(currentUser.Id, request.PermissionCode, cancellationToken))
            throw new ConflictException("Ya tienes una solicitud pendiente de ese permiso.");

        if (await grants.ExistsAsync(currentUser.Id, request.PermissionCode, cancellationToken))
            throw new ConflictException("Ya tienes concedido ese permiso.");

        var permissionRequest = PermissionRequest.Create(
            currentUser.Id,
            request.PermissionCode,
            clock.UtcNow);

        await requests.AddAsync(permissionRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return permissionRequest.Id;
    }
}
