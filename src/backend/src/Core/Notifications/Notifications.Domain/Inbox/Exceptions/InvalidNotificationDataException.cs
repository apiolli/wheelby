using SharedKernel.Exceptions;

namespace Notifications.Domain.Inbox.Exceptions;

public sealed class InvalidNotificationDataException(string message)
    : AppException(message, "notification.invalid-data")
{
    public override int StatusCode => 400;
}
