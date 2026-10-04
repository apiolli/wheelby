using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class InvalidCredentialsException()
    : AppException("Correo o contraseña incorrectos.")
{
    public override int StatusCode => 401;
}