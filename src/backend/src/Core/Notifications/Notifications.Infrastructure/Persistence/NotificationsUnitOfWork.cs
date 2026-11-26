using Notifications.Application.Abstractions;

namespace Notifications.Infrastructure.Persistence;

// Un SaveChanges de EF es una sola transacción: lo que se agregó al DbContext
// (notificaciones y correos) se guarda junto o no se guarda.
internal sealed class NotificationsUnitOfWork(NotificationsDbContext db) : INotificationsUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
