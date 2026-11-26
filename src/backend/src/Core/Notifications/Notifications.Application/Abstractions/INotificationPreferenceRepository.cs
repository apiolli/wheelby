using Notifications.Domain.Inbox;

namespace Notifications.Application.Abstractions;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken);

    // Usuarios de la lista que desactivaron el correo. Los que no tienen fila lo tienen activado.
    Task<IReadOnlySet<Guid>> ListEmailDisabledAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
