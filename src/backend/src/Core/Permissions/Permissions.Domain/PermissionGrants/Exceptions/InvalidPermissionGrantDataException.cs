using SharedKernel.Exceptions;

namespace Permissions.Domain.PermissionGrants.Exceptions;

// 400: datos inválidos al crear la concesión.
public sealed class InvalidPermissionGrantDataException(string message)
    : BadRequestException(message);
