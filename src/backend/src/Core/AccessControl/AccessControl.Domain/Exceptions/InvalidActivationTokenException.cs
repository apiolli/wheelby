using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class InvalidActivationTokenException()
    : AppException("El enlace de activación no es válido o ha vencido.")
{
    public override int StatusCode => StatusCodes.BadRequest;

    private static class StatusCodes
    {
        public const int BadRequest = 400;
    }
}