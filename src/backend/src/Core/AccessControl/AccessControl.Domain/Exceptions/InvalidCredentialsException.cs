using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class InvalidCredentialsException()
    : AppException("El correo o la contraseña no son correctos. Revisa tus datos e inténtalo de nuevo.")
{
    public override int StatusCode => 401;
}