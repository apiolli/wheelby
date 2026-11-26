using AccessControl.Contracts;
using MediatR;
using Notifications.Application.Features.PublishNotification;
using Notifications.Contracts;
using INotificationPublisher = Notifications.Contracts.INotificationPublisher;

namespace Notifications.Infrastructure.Inbox;

// Implementación de INotificationPublisher sobre ISender, como EmailQueue.
// No usa ICurrentUser: funciona igual en la API y en el Worker (expiración de reservas).
internal sealed class NotificationPublisher(
    ISender sender,
    IAdministratorDirectory administrators) : INotificationPublisher
{
    public async Task PublishAsync(
        NotificationMessage message,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (recipientIds.Count == 0)
            return;

        await sender.Send(new PublishNotificationCommand(
            message.Type, message.Title, message.Message, message.ReferenceId, recipientIds), cancellationToken);
    }

    public async Task PublishToAdministratorsAsync(
        NotificationMessage message,
        IReadOnlyCollection<Guid> excludeUserIds,
        CancellationToken cancellationToken = default)
    {
        var admins = await administrators.ListActiveAdministratorsAsync(cancellationToken);
        var recipients = admins
            .Select(a => a.Id)
            .Where(id => !excludeUserIds.Contains(id))
            .ToArray();

        await PublishAsync(message, recipients, cancellationToken);
    }
}
