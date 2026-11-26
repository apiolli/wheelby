namespace Notifications.Application.Abstractions;

// Unidad de trabajo del módulo: un solo SaveChanges, una sola transacción por operación.
// Notificación y correo se guardan juntos (RF-NOT-06).
public interface INotificationsUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
