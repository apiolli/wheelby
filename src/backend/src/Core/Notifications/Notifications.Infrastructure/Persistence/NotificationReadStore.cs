using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Application.DTOs;

namespace Notifications.Infrastructure.Persistence;

internal sealed class NotificationReadStore(NotificationsDbContext db) : INotificationReadStore
{
    public async Task<NotificationPageDTO> ListForRecipientAsync(
        Guid recipientId, bool? read, int page, int pageSize, CancellationToken cancellationToken)
    {
        var own = db.Notifications.AsNoTracking().Where(n => n.RecipientId == recipientId);

        var unread = await own.CountAsync(n => n.ReadAt == null, cancellationToken);

        var filtered = read switch
        {
            true => own.Where(n => n.ReadAt != null),
            false => own.Where(n => n.ReadAt == null),
            null => own
        };

        var total = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .OrderByDescending(n => n.CreatedAt).ThenBy(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDTO(
                n.Id,
                n.RecipientId,
                n.Type,
                n.Title,
                n.Message,
                n.ReferenceId,
                n.ReadAt != null,
                n.ReadAt,
                n.CreatedAt,
                n.EmailId != null))
            .ToListAsync(cancellationToken);

        return new NotificationPageDTO(items, page, pageSize, total, unread);
    }
}
