using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class CannotDisableSelfException()
    : AppException("No puedes desactivar tu propia cuenta.")
{
    public override int StatusCode => 403;
}