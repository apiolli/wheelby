using SharedKernel.Exceptions;

namespace Permissions.Domain.PermissionRequests.Exceptions;

// 403: la transición existe pero este actor no puede ejecutarla.
public sealed class PermissionRequestTransitionNotAllowedException(string message)
    : ForbiddenException(message);
