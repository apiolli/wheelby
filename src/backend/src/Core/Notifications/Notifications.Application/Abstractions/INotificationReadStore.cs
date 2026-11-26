using Notifications.Application.DTOs;

namespace Notifications.Application.Abstractions;

// Lecturas de la bandeja. Siempre filtradas por destinatario: no hay forma de pedir las de otro.
public interface INotificationReadStore
{
    Task<NotificationPageDTO> ListForRecipientAsync(
        Guid recipientId, bool? read, int page, int pageSize, CancellationToken cancellationToken);
}
