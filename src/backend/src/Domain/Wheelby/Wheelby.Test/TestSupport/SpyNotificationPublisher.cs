using Notifications.Contracts;

namespace Wheelby.Test.TestSupport;

// Espía del motor de notificaciones: registra cada publicación o falla si se le pide.
public sealed class SpyNotificationPublisher : INotificationPublisher
{
    public List<(NotificationMessage Message, IReadOnlyCollection<Guid> RecipientIds)> Published { get; } = [];
    public int ToAdministratorsCalls { get; private set; }
    public Exception? FailWith { get; set; }

    public Task PublishAsync(
        NotificationMessage message, IReadOnlyCollection<Guid> recipientIds, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null) throw FailWith;
        Published.Add((message, recipientIds.ToArray()));
        return Task.CompletedTask;
    }

    public Task PublishToAdministratorsAsync(
        NotificationMessage message, IReadOnlyCollection<Guid> excludeUserIds, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null) throw FailWith;
        ToAdministratorsCalls++;
        return Task.CompletedTask;
    }
}
