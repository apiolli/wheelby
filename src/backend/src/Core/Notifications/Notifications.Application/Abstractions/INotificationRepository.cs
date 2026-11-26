using Notifications.Domain.Inbox;

namespace Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    // Busca por id y por destinatario a la vez: la de otro usuario se ve igual que una que no existe.
    Task<Notification?> GetForRecipientAsync(Guid id, Guid recipientId, CancellationToken cancellationToken);
}
