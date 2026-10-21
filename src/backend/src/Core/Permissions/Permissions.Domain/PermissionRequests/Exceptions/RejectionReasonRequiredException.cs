using SharedKernel.Exceptions;

namespace Permissions.Domain.PermissionRequests.Exceptions;

// 400: el rechazo de una solicitud exige un motivo.
public sealed class RejectionReasonRequiredException(string message)
    : BadRequestException(message);
