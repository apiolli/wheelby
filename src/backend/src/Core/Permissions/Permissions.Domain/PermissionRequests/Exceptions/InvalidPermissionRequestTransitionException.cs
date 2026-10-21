using SharedKernel.Exceptions;

namespace Permissions.Domain.PermissionRequests.Exceptions;

// 409: el par (origen, destino) no existe en la tabla, transición prohibida u origen terminal.
public sealed class InvalidPermissionRequestTransitionException(string message)
    : ConflictException(message);
