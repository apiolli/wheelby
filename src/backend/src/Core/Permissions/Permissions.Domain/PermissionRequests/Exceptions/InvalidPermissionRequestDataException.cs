using SharedKernel.Exceptions;

namespace Permissions.Domain.PermissionRequests.Exceptions;

// 400: datos inválidos al crear la solicitud (código mal formado, ids vacíos, fecha no UTC).
public sealed class InvalidPermissionRequestDataException(string message)
    : BadRequestException(message);
