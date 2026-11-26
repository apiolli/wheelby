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
    IClock clock,
    // RF-NOT-01: opcional y al final para que las pruebas previas sigan compilando sin cambios.
    // En la API siempre está registrado.
    IPermissionRequestNotifier? notifier = null) : IRequestHandler<CreatePermissionRequestCommand, Guid>
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

        // Efecto secundario posterior al guardado: un fallo se registra y no cambia la respuesta.
        if (notifier is not null)
            await notifier.RequestCreatedAsync(permissionRequest, cancellationToken);

        return permissionRequest.Id;
    }
}
