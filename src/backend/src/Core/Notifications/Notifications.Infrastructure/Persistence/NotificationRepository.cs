using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Domain.Inbox;

namespace Notifications.Infrastructure.Persistence;

internal sealed class NotificationRepository(NotificationsDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
        => await db.Notifications.AddAsync(notification, cancellationToken);

    public Task<Notification?> GetForRecipientAsync(Guid id, Guid recipientId, CancellationToken cancellationToken)
        => db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == recipientId, cancellationToken);
}
