using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class AccountLockedException()
    : AppException("La cuenta está bloqueada temporalmente por demasiados intentos fallidos. Inténtalo más tarde.")
{
    public override int StatusCode => 403;
}