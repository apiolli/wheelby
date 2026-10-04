using SharedKernel.Exceptions;

namespace AccessControl.Domain.Sessions.Exceptions;

public sealed class InvalidSessionException()
    : AppException("La sesión no es válida o ha expirado.")
{
    public override int StatusCode => 401;
}