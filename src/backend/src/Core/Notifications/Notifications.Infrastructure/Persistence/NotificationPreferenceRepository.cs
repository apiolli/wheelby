using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Domain.Inbox;

namespace Notifications.Infrastructure.Persistence;

internal sealed class NotificationPreferenceRepository(NotificationsDbContext db) : INotificationPreferenceRepository
{
    public Task<NotificationPreference?> GetAsync(Guid userId, CancellationToken cancellationToken)
        => db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken)
        => await db.NotificationPreferences.AddAsync(preference, cancellationToken);

    public async Task<IReadOnlySet<Guid>> ListEmailDisabledAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        // AGENTS.md §8: arreglo en Contains, para que Npgsql lo traduzca a ANY.
        var ids = userIds.ToArray();

        var disabled = await db.NotificationPreferences
            .AsNoTracking()
            .Where(p => ids.Contains(p.UserId) && !p.EmailEnabled)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        return disabled.ToHashSet();
    }
}
