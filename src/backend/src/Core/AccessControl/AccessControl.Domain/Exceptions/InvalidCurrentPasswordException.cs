using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class InvalidCurrentPasswordException()
    : BadRequestException("La contraseña actual es incorrecta.");
