using SharedKernel.Exceptions;

namespace Permissions.Domain.PermissionRequests.Exceptions;

// 403: un usuario no puede resolver su propia solicitud de permiso.
public sealed class CannotResolveOwnPermissionRequestException(string message)
    : ForbiddenException(message);
