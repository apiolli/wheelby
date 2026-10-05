using SharedKernel.Exceptions;

namespace AccessControl.Domain.Users.Exceptions;

public sealed class InvalidUserDataException(string message) : AppException(message)
{
    public override int StatusCode => 400;
}