using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class InvalidPasswordResetTokenException()
    : BadRequestException("El código de recuperación no es válido o ha vencido.");
