using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class AccountDisabledException()
    : AppException("La cuenta está desactivada. Contacta a un administrador.")
{
    public override int StatusCode => 403;
}