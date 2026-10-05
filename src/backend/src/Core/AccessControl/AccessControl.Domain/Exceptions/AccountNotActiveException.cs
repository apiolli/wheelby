using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class AccountNotActiveException()
    : AppException("La cuenta no está activa. Abre el enlace de activación que enviamos a tu correo.")
{
    public override int StatusCode => 403;
}